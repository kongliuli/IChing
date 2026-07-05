package com.iching.server;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;

import java.time.LocalDate;

import static org.junit.jupiter.api.Assertions.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
class MvpFlowIntegrationTest {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private ObjectMapper objectMapper;

    @Test
    void milestoneAtoD() throws Exception {
        String phone = "138" + System.currentTimeMillis() % 100000000;

        MvcResult register = mockMvc.perform(post("/api/user/register")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("""
                                {"phone":"%s","password":"secret12","nickname":"tester"}
                                """.formatted(phone)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.code").value(0))
                .andReturn();

        String token = read(register, "token");

        mockMvc.perform(put("/api/user/profile")
                        .header("Authorization", "Bearer " + token)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("""
                                {"birthday":"1990-05-20","birthHour":10,"gender":1,"province":"广东","city":"深圳"}
                                """))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.data.birthday").value("1990-05-20"));

        MvcResult calc = mockMvc.perform(post("/api/bazi/calculate")
                        .header("Authorization", "Bearer " + token)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("""
                                {"title":"我的命盘","birthday":"1990-05-20","birthHour":10,"gender":1,"province":"广东","city":"深圳"}
                                """))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.data.deepReportLocked").value(true))
                .andReturn();

        long paipanId = readLong(calc, "paipanId");

        mockMvc.perform(get("/api/bazi/history")
                        .header("Authorization", "Bearer " + token))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.data[0].paipanId").value((int) paipanId));

        MvcResult order = mockMvc.perform(post("/api/order/create")
                        .header("Authorization", "Bearer " + token)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("""
                                {"productType":1,"paipanId":%d}
                                """.formatted(paipanId)))
                .andExpect(status().isOk())
                .andReturn();

        String orderNo = read(order, "orderNo");

        mockMvc.perform(post("/api/order/pay")
                        .header("Authorization", "Bearer " + token)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("""
                                {"orderNo":"%s"}
                                """.formatted(orderNo)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.data.status").value(1));

        mockMvc.perform(get("/api/bazi/" + paipanId)
                        .header("Authorization", "Bearer " + token))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.data.deepReportLocked").value(false))
                .andExpect(jsonPath("$.data.deepReport.content").exists());

        MvcResult adminLogin = mockMvc.perform(post("/api/user/login")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("""
                                {"phone":"10000000000","password":"admin123"}
                                """))
                .andExpect(status().isOk())
                .andReturn();

        String adminToken = read(adminLogin, "token");

        mockMvc.perform(get("/admin/users")
                        .header("Authorization", "Bearer " + adminToken))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.data").isArray());
    }

    private String read(MvcResult result, String field) throws Exception {
        JsonNode node = objectMapper.readTree(result.getResponse().getContentAsString());
        return node.path("data").path(field).asText();
    }

    private long readLong(MvcResult result, String field) throws Exception {
        JsonNode node = objectMapper.readTree(result.getResponse().getContentAsString());
        return node.path("data").path(field).asLong();
    }
}
