package com.iching.server.web;

import com.iching.common.ApiResponse;
import com.iching.server.dto.LoginRequest;
import com.iching.server.dto.ProfileUpdateRequest;
import com.iching.server.dto.RegisterRequest;
import com.iching.server.security.AuthContext;
import com.iching.server.service.UserService;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/user")
public class UserController {

    private final UserService userService;

    public UserController(UserService userService) {
        this.userService = userService;
    }

    @PostMapping("/register")
    public ApiResponse<?> register(@Valid @RequestBody RegisterRequest request) {
        return ApiResponse.ok(userService.register(request));
    }

    @PostMapping("/login")
    public ApiResponse<?> login(@Valid @RequestBody LoginRequest request) {
        return ApiResponse.ok(userService.login(request));
    }

    @GetMapping("/profile")
    public ApiResponse<?> profile() {
        return ApiResponse.ok(userService.getProfile(AuthContext.currentUserId()));
    }

    @PutMapping("/profile")
    public ApiResponse<?> updateProfile(@Valid @RequestBody ProfileUpdateRequest request) {
        return ApiResponse.ok(userService.updateProfile(AuthContext.currentUserId(), request));
    }
}
