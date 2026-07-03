package com.iching.server.repository;

import com.iching.server.domain.PaipanEntity;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface PaipanRepository extends JpaRepository<PaipanEntity, Long> {
    List<PaipanEntity> findByUserIdOrderByCreateTimeDesc(Long userId);
}
