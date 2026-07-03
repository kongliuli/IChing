package com.iching.server.dto;

import jakarta.validation.constraints.NotNull;

public record CreateOrderRequest(
        @NotNull Integer productType,
        Long paipanId
) {
}
