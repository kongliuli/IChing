package com.iching.server.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.iching.common.BizException;
import com.iching.paipan.BaziCalculator;
import com.iching.paipan.BaziResult;
import com.iching.paipan.BirthInfo;
import com.iching.server.domain.PaipanEntity;
import com.iching.server.domain.PaipanReportEntity;
import com.iching.server.dto.BaziCalculateRequest;
import com.iching.server.repository.PaipanReportRepository;
import com.iching.server.repository.PaipanRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Service
public class BaziService {

    private final PaipanRepository paipanRepository;
    private final PaipanReportRepository reportRepository;
    private final BaziCalculator baziCalculator;
    private final ObjectMapper objectMapper;

    public BaziService(
            PaipanRepository paipanRepository,
            PaipanReportRepository reportRepository,
            BaziCalculator baziCalculator,
            ObjectMapper objectMapper) {
        this.paipanRepository = paipanRepository;
        this.reportRepository = reportRepository;
        this.baziCalculator = baziCalculator;
        this.objectMapper = objectMapper;
    }

    @Transactional
    public Map<String, Object> calculate(long userId, BaziCalculateRequest request) {
        BirthInfo birthInfo = new BirthInfo(
                request.birthday(),
                request.birthHour(),
                request.gender(),
                request.province() != null ? request.province() : "",
                request.city() != null ? request.city() : ""
        );
        BaziResult result = baziCalculator.calculate(birthInfo);

        PaipanEntity entity = new PaipanEntity();
        entity.setUserId(userId);
        entity.setType(1);
        entity.setTitle(request.title() != null ? request.title() : "八字排盘");
        entity.setBirthData(toMap(birthInfo));
        entity.setResultData(toMap(result));
        entity.setIsPaid(0);
        paipanRepository.save(entity);

        Map<String, Object> response = new HashMap<>();
        response.put("paipanId", entity.getPaipanId());
        response.put("basic", entity.getResultData());
        response.put("deepReportLocked", true);
        return response;
    }

    public List<Map<String, Object>> history(long userId) {
        return paipanRepository.findByUserIdOrderByCreateTimeDesc(userId).stream()
                .map(p -> Map.<String, Object>of(
                        "paipanId", p.getPaipanId(),
                        "title", p.getTitle(),
                        "isPaid", p.getIsPaid(),
                        "createTime", p.getCreateTime().toString()
                ))
                .toList();
    }

    public Map<String, Object> detail(long userId, long paipanId) {
        PaipanEntity paipan = paipanRepository.findById(paipanId)
                .orElseThrow(() -> new BizException(2001, "paipan not found"));
        if (!paipan.getUserId().equals(userId)) {
            throw new BizException(2002, "forbidden");
        }

        Map<String, Object> response = new HashMap<>();
        response.put("paipanId", paipan.getPaipanId());
        response.put("title", paipan.getTitle());
        response.put("birthData", paipan.getBirthData());
        response.put("basic", paipan.getResultData());
        response.put("isPaid", paipan.getIsPaid());

        if (paipan.getIsPaid() != null && paipan.getIsPaid() == 1) {
            reportRepository.findByPaipanId(paipanId).ifPresent(report -> {
                response.put("deepReport", Map.of(
                        "content", report.getContent(),
                        "status", report.getStatus()
                ));
            });
            response.put("deepReportLocked", false);
        } else {
            response.put("deepReportLocked", true);
        }
        return response;
    }

    @Transactional
    public void unlockDeepReport(long userId, long paipanId, long orderId) {
        PaipanEntity paipan = paipanRepository.findById(paipanId)
                .orElseThrow(() -> new BizException(2001, "paipan not found"));
        if (!paipan.getUserId().equals(userId)) {
            throw new BizException(2002, "forbidden");
        }
        paipan.setIsPaid(1);

        PaipanReportEntity report = reportRepository.findByPaipanId(paipanId).orElseGet(() -> {
            PaipanReportEntity r = new PaipanReportEntity();
            r.setPaipanId(paipanId);
            r.setUserId(userId);
            return r;
        });
        report.setOrderId(orderId);
        report.setContent("深度报告：基于您的命盘，事业与财运呈现稳步上升趋势。（MVP 模板内容）");
        report.setStatus(1);
        reportRepository.save(report);

        paipan.setReportId(report.getReportId());
        paipanRepository.save(paipan);
    }

    private Map<String, Object> toMap(BirthInfo birthInfo) {
        Map<String, Object> map = new HashMap<>();
        map.put("birthday", birthInfo.birthday().toString());
        map.put("birthHour", birthInfo.birthHour());
        map.put("gender", birthInfo.gender());
        map.put("province", birthInfo.province());
        map.put("city", birthInfo.city());
        return map;
    }

    private Map<String, Object> toMap(BaziResult result) {
        return objectMapper.convertValue(result, Map.class);
    }
}
