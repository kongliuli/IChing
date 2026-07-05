package com.iching.server.web;

import com.iching.common.ApiResponse;
import com.iching.server.dto.CreateOrderRequest;
import com.iching.server.dto.PayOrderRequest;
import com.iching.server.security.AuthContext;
import com.iching.server.service.OrderService;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/order")
public class OrderController {

    private final OrderService orderService;

    public OrderController(OrderService orderService) {
        this.orderService = orderService;
    }

    @PostMapping("/create")
    public ApiResponse<?> create(@Valid @RequestBody CreateOrderRequest request) {
        return ApiResponse.ok(orderService.createOrder(AuthContext.currentUserId(), request));
    }

    @PostMapping("/pay")
    public ApiResponse<?> pay(@Valid @RequestBody PayOrderRequest request) {
        return ApiResponse.ok(orderService.mockPay(AuthContext.currentUserId(), request.orderNo()));
    }

    @GetMapping("/{orderNo}")
    public ApiResponse<?> get(@PathVariable String orderNo) {
        return ApiResponse.ok(orderService.getOrder(AuthContext.currentUserId(), orderNo));
    }
}
