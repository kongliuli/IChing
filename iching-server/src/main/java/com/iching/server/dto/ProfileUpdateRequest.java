package com.iching.server.dto;

import java.time.LocalDate;

public record ProfileUpdateRequest(
        String nickname,
        LocalDate birthday,
        Integer birthHour,
        Integer gender,
        String province,
        String city
) {
}
