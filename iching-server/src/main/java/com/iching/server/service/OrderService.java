package com.iching.server.service;

import com.iching.common.BizException;
import com.iching.server.domain.OrderEntity;
import com.iching.server.domain.OrderItemEntity;
import com.iching.server.dto.CreateOrderRequest;
import com.iching.server.repository.OrderItemRepository;
import com.iching.server.repository.OrderRepository;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.Map;
import java.util.UUID;

@Service
public class OrderService {

    public static final int PRODUCT_DEEP_REPORT = 1;
    public static final int PRODUCT_MEMBERSHIP = 2;

    private final OrderRepository orderRepository;
    private final OrderItemRepository orderItemRepository;
    private final UserService userService;
    private final BaziService baziService;
    private final BigDecimal deepReportPrice;
    private final BigDecimal membershipPrice;

    public OrderService(
            OrderRepository orderRepository,
            OrderItemRepository orderItemRepository,
            UserService userService,
            BaziService baziService,
            @Value("${iching.pricing.deep-report}") BigDecimal deepReportPrice,
            @Value("${iching.pricing.membership-year}") BigDecimal membershipPrice) {
        this.orderRepository = orderRepository;
        this.orderItemRepository = orderItemRepository;
        this.userService = userService;
        this.baziService = baziService;
        this.deepReportPrice = deepReportPrice;
        this.membershipPrice = membershipPrice;
    }

    @Transactional
    public Map<String, Object> createOrder(long userId, CreateOrderRequest request) {
        OrderEntity order = new OrderEntity();
        order.setOrderNo(generateOrderNo());
        order.setUserId(userId);
        order.setStatus(0);

        OrderItemEntity item = new OrderItemEntity();
        item.setQuantity(1);

        if (request.productType() == PRODUCT_DEEP_REPORT) {
            if (request.paipanId() == null) {
                throw new BizException(3001, "paipanId required for deep report");
            }
            item.setProductId(1001L);
            item.setProductName("八字深度报告");
            item.setProductType(PRODUCT_DEEP_REPORT);
            item.setRefId(request.paipanId());
            item.setPrice(deepReportPrice);
            item.setTotalAmount(deepReportPrice);
            order.setTotalAmount(deepReportPrice);
        } else if (request.productType() == PRODUCT_MEMBERSHIP) {
            item.setProductId(2001L);
            item.setProductName("年费会员");
            item.setProductType(PRODUCT_MEMBERSHIP);
            item.setPrice(membershipPrice);
            item.setTotalAmount(membershipPrice);
            order.setTotalAmount(membershipPrice);
        } else {
            throw new BizException(3002, "unsupported product type");
        }

        orderRepository.save(order);
        item.setOrderId(order.getOrderId());
        orderItemRepository.save(item);

        return Map.of(
                "orderNo", order.getOrderNo(),
                "totalAmount", order.getTotalAmount(),
                "status", order.getStatus()
        );
    }

    @Transactional
    public Map<String, Object> mockPay(long userId, String orderNo) {
        OrderEntity order = orderRepository.findByOrderNo(orderNo)
                .orElseThrow(() -> new BizException(3003, "order not found"));
        if (!order.getUserId().equals(userId)) {
            throw new BizException(3004, "forbidden");
        }
        if (order.getStatus() != null && order.getStatus() == 1) {
            return toOrderMap(order);
        }

        order.setStatus(1);
        order.setPayType(3);
        order.setPayAmount(order.getTotalAmount());
        order.setPayTime(LocalDateTime.now());
        orderRepository.save(order);

        for (OrderItemEntity item : orderItemRepository.findByOrderId(order.getOrderId())) {
            if (item.getProductType() == PRODUCT_DEEP_REPORT) {
                baziService.unlockDeepReport(userId, item.getRefId(), order.getOrderId());
            } else if (item.getProductType() == PRODUCT_MEMBERSHIP) {
                userService.activateMembership(userId, order);
            }
        }
        return toOrderMap(order);
    }

    public Map<String, Object> getOrder(long userId, String orderNo) {
        OrderEntity order = orderRepository.findByOrderNo(orderNo)
                .orElseThrow(() -> new BizException(3003, "order not found"));
        if (!order.getUserId().equals(userId)) {
            throw new BizException(3004, "forbidden");
        }
        return toOrderMap(order);
    }

    private Map<String, Object> toOrderMap(OrderEntity order) {
        return Map.of(
                "orderNo", order.getOrderNo(),
                "status", order.getStatus(),
                "totalAmount", order.getTotalAmount(),
                "payAmount", order.getPayAmount() != null ? order.getPayAmount() : BigDecimal.ZERO,
                "payTime", order.getPayTime() != null ? order.getPayTime().toString() : ""
        );
    }

    private String generateOrderNo() {
        return "IC" + System.currentTimeMillis() + UUID.randomUUID().toString().substring(0, 6).toUpperCase();
    }
}
