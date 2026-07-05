package com.iching.server.dto;

import java.time.LocalDate;
import java.time.LocalDateTime;

public record UserProfileResponse(
        Long userId,
        String phone,
        String nickname,
        LocalDate birthday,
        Integer birthHour,
        Integer gender,
        String province,
        String city,
        Integer memberLevel,
        LocalDateTime memberExpireTime
) {
}
