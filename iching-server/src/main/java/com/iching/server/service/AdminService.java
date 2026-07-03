package com.iching.server.service;

import com.iching.server.domain.OrderEntity;
import com.iching.server.domain.PaipanEntity;
import com.iching.server.domain.UserEntity;
import com.iching.server.dto.MemberUpdateRequest;
import com.iching.server.dto.UserProfileResponse;
import com.iching.server.repository.OrderRepository;
import com.iching.server.repository.PaipanRepository;
import com.iching.server.repository.UserRepository;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Map;

@Service
public class AdminService {

    private final UserRepository userRepository;
    private final OrderRepository orderRepository;
    private final PaipanRepository paipanRepository;
    private final UserService userService;

    public AdminService(
            UserRepository userRepository,
            OrderRepository orderRepository,
            PaipanRepository paipanRepository,
            UserService userService) {
        this.userRepository = userRepository;
        this.orderRepository = orderRepository;
        this.paipanRepository = paipanRepository;
        this.userService = userService;
    }

    public List<UserProfileResponse> listUsers() {
        return userRepository.findAll().stream().map(this::toProfile).toList();
    }

    public List<Map<String, Object>> listOrders() {
        return orderRepository.findAll().stream().map(this::toOrder).toList();
    }

    public List<Map<String, Object>> listPaipans() {
        return paipanRepository.findAll().stream().map(this::toPaipan).toList();
    }

    public UserProfileResponse updateMember(long userId, MemberUpdateRequest request) {
        return userService.updateMember(userId, request);
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

    private Map<String, Object> toOrder(OrderEntity order) {
        return Map.of(
                "orderNo", order.getOrderNo(),
                "userId", order.getUserId(),
                "status", order.getStatus(),
                "totalAmount", order.getTotalAmount()
        );
    }

    private Map<String, Object> toPaipan(PaipanEntity paipan) {
        return Map.of(
                "paipanId", paipan.getPaipanId(),
                "userId", paipan.getUserId(),
                "title", paipan.getTitle(),
                "isPaid", paipan.getIsPaid(),
                "createTime", paipan.getCreateTime().toString()
        );
    }
}
