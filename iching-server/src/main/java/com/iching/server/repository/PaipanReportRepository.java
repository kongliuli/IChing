package com.iching.server.repository;

import com.iching.server.domain.PaipanReportEntity;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface PaipanReportRepository extends JpaRepository<PaipanReportEntity, Long> {
    Optional<PaipanReportEntity> findByPaipanId(Long paipanId);
}
