package com.iching.server.web;

import com.iching.common.ApiResponse;
import com.iching.server.dto.MemberUpdateRequest;
import com.iching.server.service.AdminService;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/admin")
public class AdminController {

    private final AdminService adminService;

    public AdminController(AdminService adminService) {
        this.adminService = adminService;
    }

    @GetMapping("/users")
    public ApiResponse<?> users() {
        return ApiResponse.ok(adminService.listUsers());
    }

    @GetMapping("/orders")
    public ApiResponse<?> orders() {
        return ApiResponse.ok(adminService.listOrders());
    }

    @GetMapping("/paipans")
    public ApiResponse<?> paipans() {
        return ApiResponse.ok(adminService.listPaipans());
    }

    @PutMapping("/users/{userId}/member")
    public ApiResponse<?> updateMember(@PathVariable long userId, @Valid @RequestBody MemberUpdateRequest request) {
        return ApiResponse.ok(adminService.updateMember(userId, request));
    }
}
