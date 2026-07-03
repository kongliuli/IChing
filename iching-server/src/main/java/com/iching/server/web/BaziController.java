package com.iching.server.web;

import com.iching.common.ApiResponse;
import com.iching.server.dto.BaziCalculateRequest;
import com.iching.server.security.AuthContext;
import com.iching.server.service.BaziService;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/bazi")
public class BaziController {

    private final BaziService baziService;

    public BaziController(BaziService baziService) {
        this.baziService = baziService;
    }

    @PostMapping("/calculate")
    public ApiResponse<?> calculate(@Valid @RequestBody BaziCalculateRequest request) {
        return ApiResponse.ok(baziService.calculate(AuthContext.currentUserId(), request));
    }

    @GetMapping("/history")
    public ApiResponse<?> history() {
        return ApiResponse.ok(baziService.history(AuthContext.currentUserId()));
    }

    @GetMapping("/{paipanId}")
    public ApiResponse<?> detail(@PathVariable long paipanId) {
        return ApiResponse.ok(baziService.detail(AuthContext.currentUserId(), paipanId));
    }
}
