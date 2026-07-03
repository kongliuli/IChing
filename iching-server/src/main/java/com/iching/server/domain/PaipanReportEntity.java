package com.iching.server.domain;

import jakarta.persistence.*;
import java.time.LocalDateTime;

@Entity
@Table(name = "t_paipan_report")
public class PaipanReportEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "report_id")
    private Long reportId;

    @Column(name = "paipan_id")
    private Long paipanId;

    @Column(name = "order_id")
    private Long orderId;

    @Column(name = "user_id")
    private Long userId;

    @Lob
    private String content;

    @Column(name = "ai_interpretation")
    private String aiInterpretation;

    private Integer status = 0;

    @Column(name = "create_time")
    private LocalDateTime createTime;

    @PrePersist
    void prePersist() {
        createTime = LocalDateTime.now();
    }

    public Long getReportId() { return reportId; }
    public void setReportId(Long reportId) { this.reportId = reportId; }
    public Long getPaipanId() { return paipanId; }
    public void setPaipanId(Long paipanId) { this.paipanId = paipanId; }
    public Long getOrderId() { return orderId; }
    public void setOrderId(Long orderId) { this.orderId = orderId; }
    public Long getUserId() { return userId; }
    public void setUserId(Long userId) { this.userId = userId; }
    public String getContent() { return content; }
    public void setContent(String content) { this.content = content; }
    public String getAiInterpretation() { return aiInterpretation; }
    public void setAiInterpretation(String aiInterpretation) { this.aiInterpretation = aiInterpretation; }
    public Integer getStatus() { return status; }
    public void setStatus(Integer status) { this.status = status; }
    public LocalDateTime getCreateTime() { return createTime; }
}
