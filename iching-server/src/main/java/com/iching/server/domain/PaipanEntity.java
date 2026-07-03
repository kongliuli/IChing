package com.iching.server.domain;

import jakarta.persistence.*;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

import java.time.LocalDateTime;
import java.util.Map;

@Entity
@Table(name = "t_paipan")
public class PaipanEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "paipan_id")
    private Long paipanId;

    @Column(name = "user_id")
    private Long userId;

    private Integer type = 1;
    private String title;

    @JdbcTypeCode(SqlTypes.JSON)
    @Column(name = "birth_data", columnDefinition = "json")
    private Map<String, Object> birthData;

    @JdbcTypeCode(SqlTypes.JSON)
    @Column(name = "result_data", columnDefinition = "json")
    private Map<String, Object> resultData;

    @Column(name = "is_paid")
    private Integer isPaid = 0;

    @Column(name = "report_id")
    private Long reportId;

    @Column(name = "create_time")
    private LocalDateTime createTime;

    @PrePersist
    void prePersist() {
        createTime = LocalDateTime.now();
    }

    public Long getPaipanId() { return paipanId; }
    public void setPaipanId(Long paipanId) { this.paipanId = paipanId; }
    public Long getUserId() { return userId; }
    public void setUserId(Long userId) { this.userId = userId; }
    public Integer getType() { return type; }
    public void setType(Integer type) { this.type = type; }
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    public Map<String, Object> getBirthData() { return birthData; }
    public void setBirthData(Map<String, Object> birthData) { this.birthData = birthData; }
    public Map<String, Object> getResultData() { return resultData; }
    public void setResultData(Map<String, Object> resultData) { this.resultData = resultData; }
    public Integer getIsPaid() { return isPaid; }
    public void setIsPaid(Integer isPaid) { this.isPaid = isPaid; }
    public Long getReportId() { return reportId; }
    public void setReportId(Long reportId) { this.reportId = reportId; }
    public LocalDateTime getCreateTime() { return createTime; }
}
