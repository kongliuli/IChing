package com.iching.server.config;

import com.iching.common.AesCrypto;
import com.iching.common.JwtService;
import com.iching.paipan.BaziCalculator;
import com.iching.paipan.PlaceholderBaziCalculator;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class AppConfig {

    @Bean
    JwtService jwtService(
            @Value("${iching.jwt.secret}") String secret,
            @Value("${iching.jwt.ttl-seconds}") long ttlSeconds) {
        return new JwtService(secret, ttlSeconds);
    }

    @Bean
    AesCrypto aesCrypto(@Value("${iching.aes.secret}") String secret) {
        return new AesCrypto(secret);
    }

    @Bean
    BaziCalculator baziCalculator() {
        return new PlaceholderBaziCalculator();
    }
}
