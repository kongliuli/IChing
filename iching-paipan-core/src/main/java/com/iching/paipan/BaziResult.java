package com.iching.paipan;

import java.util.List;
import java.util.Map;

public record BaziResult(
        List<String> yearPillar,
        List<String> monthPillar,
        List<String> dayPillar,
        List<String> hourPillar,
        Map<String, Object> meta
) {
}
