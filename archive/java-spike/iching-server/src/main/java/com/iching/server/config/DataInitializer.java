package com.iching.server.config;

import com.iching.server.domain.UserEntity;
import com.iching.server.repository.UserRepository;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.crypto.password.PasswordEncoder;

@Configuration
public class DataInitializer {

    @Bean
    ApplicationRunner seedAdmin(UserRepository userRepository, PasswordEncoder passwordEncoder) {
        return args -> {
            if (userRepository.findByPhone("10000000000").isEmpty()) {
                UserEntity admin = new UserEntity();
                admin.setPhone("10000000000");
                admin.setPassword(passwordEncoder.encode("admin123"));
                admin.setNickname("admin");
                admin.setRole("ADMIN");
                admin.setMemberLevel(1);
                userRepository.save(admin);
            }
        };
    }
}
