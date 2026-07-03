package com.iching.server.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record RegisterRequest(
        @NotBlank String phone,
        @NotBlank @Size(min = 6, max = 64) String password,
        String nickname
) {
}
