package com.iching.server.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;

public record BaziCalculateRequest(
        String title,
        @NotNull LocalDate birthday,
        @NotNull @Min(0) @Max(23) Integer birthHour,
        @NotNull Integer gender,
        String province,
        String city
) {
}
