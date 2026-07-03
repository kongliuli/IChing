package com.iching.server.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.iching.common.AesCrypto;
import com.iching.common.BizException;
import com.iching.common.JwtService;
import com.iching.server.domain.MembershipEntity;
import com.iching.server.domain.OrderEntity;
import com.iching.server.domain.UserEntity;
import com.iching.server.dto.*;
import com.iching.server.repository.MembershipRepository;
import com.iching.server.repository.UserRepository;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Map;
import java.time.LocalDateTime;

@Service
public class UserService {

    private final UserRepository userRepository;
    private final MembershipRepository membershipRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtService jwtService;
    private final AesCrypto aesCrypto;
    private final ObjectMapper objectMapper;

    public UserService(
            UserRepository userRepository,
            MembershipRepository membershipRepository,
            PasswordEncoder passwordEncoder,
            JwtService jwtService,
            AesCrypto aesCrypto,
            ObjectMapper objectMapper) {
        this.userRepository = userRepository;
        this.membershipRepository = membershipRepository;
        this.passwordEncoder = passwordEncoder;
        this.jwtService = jwtService;
        this.aesCrypto = aesCrypto;
        this.objectMapper = objectMapper;
    }

    @Transactional
    public Map<String, Object> register(RegisterRequest request) {
        if (userRepository.findByPhone(request.phone()).isPresent()) {
            throw new BizException(1001, "phone already registered");
        }
        UserEntity user = new UserEntity();
        user.setPhone(request.phone());
        user.setPassword(passwordEncoder.encode(request.password()));
        user.setNickname(request.nickname() != null ? request.nickname() : "user" + request.phone().substring(Math.max(0, request.phone().length() - 4)));
        user.setRole("USER");
        userRepository.save(user);
        return tokenResponse(user);
    }

    public Map<String, Object> login(LoginRequest request) {
        UserEntity user = userRepository.findByPhone(request.phone())
                .orElseThrow(() -> new BizException(1002, "invalid credentials"));
        if (!passwordEncoder.matches(request.password(), user.getPassword())) {
            throw new BizException(1002, "invalid credentials");
        }
        return tokenResponse(user);
    }

    public UserProfileResponse getProfile(long userId) {
        return toProfile(requireUser(userId));
    }

    @Transactional
    public UserProfileResponse updateProfile(long userId, ProfileUpdateRequest request) {
        UserEntity user = requireUser(userId);
        if (request.nickname() != null) {
            user.setNickname(request.nickname());
        }
        if (request.birthday() != null) {
            user.setBirthday(request.birthday());
        }
        if (request.birthHour() != null) {
            user.setBirthHour(request.birthHour());
        }
        if (request.gender() != null) {
            user.setGender(request.gender());
        }
        if (request.province() != null) {
            user.setProvince(request.province());
        }
        if (request.city() != null) {
            user.setCity(request.city());
        }
        if (request.birthday() != null || request.birthHour() != null) {
            user.setBirthDataEnc(encryptBirth(user));
        }
        userRepository.save(user);
        return toProfile(user);
    }

    @Transactional
    public void activateMembership(long userId, OrderEntity order) {
        UserEntity user = requireUser(userId);
        LocalDateTime now = LocalDateTime.now();
        LocalDateTime end = now.plusYears(1);
        user.setMemberLevel(1);
        user.setMemberExpireTime(end);
        userRepository.save(user);

        MembershipEntity membership = new MembershipEntity();
        membership.setUserId(userId);
        membership.setLevel(1);
        membership.setOrderId(order.getOrderId());
        membership.setStartTime(now);
        membership.setEndTime(end);
        membership.setPaymentAmount(order.getPayAmount());
        membership.setStatus(1);
        membershipRepository.save(membership);
    }

    public UserEntity requireUser(long userId) {
        return userRepository.findById(userId)
                .orElseThrow(() -> new BizException(1003, "user not found"));
    }

    @Transactional
    public UserProfileResponse updateMember(long userId, MemberUpdateRequest request) {
        UserEntity user = requireUser(userId);
        if (request.memberLevel() != null) {
            user.setMemberLevel(request.memberLevel());
        }
        if (request.expireTime() != null) {
            user.setMemberExpireTime(request.expireTime());
        }
        userRepository.save(user);
        return toProfile(user);
    }

    private Map<String, Object> tokenResponse(UserEntity user) {
        String token = jwtService.issueToken(user.getUserId(), user.getRole());
        return Map.of(
                "token", token,
                "userId", user.getUserId(),
                "role", user.getRole()
        );
    }

    private UserProfileResponse toProfile(UserEntity user) {
        return new UserProfileResponse(
                user.getUserId(),
                user.getPhone(),
                user.getNickname(),
                user.getBirthday(),
                user.getBirthHour(),
                user.getGender(),
                user.getProvince(),
                user.getCity(),
                user.getMemberLevel(),
                user.getMemberExpireTime()
        );
    }

    private String encryptBirth(UserEntity user) {
        try {
            String json = objectMapper.writeValueAsString(Map.of(
                    "birthday", user.getBirthday() != null ? user.getBirthday().toString() : "",
                    "birthHour", user.getBirthHour() != null ? user.getBirthHour() : 0,
                    "gender", user.getGender() != null ? user.getGender() : 0,
                    "province", user.getProvince() != null ? user.getProvince() : "",
                    "city", user.getCity() != null ? user.getCity() : ""
            ));
            return aesCrypto.encrypt(json);
        } catch (Exception e) {
            throw new BizException(1004, "encrypt birth data failed");
        }
    }
}
