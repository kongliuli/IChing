package com.iching.server.dto;

import java.time.LocalDateTime;

public record MemberUpdateRequest(Integer memberLevel, LocalDateTime expireTime) {
}
