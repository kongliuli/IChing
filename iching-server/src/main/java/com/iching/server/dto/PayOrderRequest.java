package com.iching.server.dto;

import jakarta.validation.constraints.NotBlank;

public record PayOrderRequest(@NotBlank String orderNo) {
}
