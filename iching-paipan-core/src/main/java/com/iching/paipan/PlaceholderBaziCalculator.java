package com.iching.paipan;

import java.util.List;
import java.util.Map;

/**
 * ponytail: placeholder deterministic calculator for MVP wiring.
 * Upgrade path: swap bean implementation without touching HTTP/DB layers.
 */
public class PlaceholderBaziCalculator implements BaziCalculator {

    private static final String[] STEMS = {"甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸"};
    private static final String[] BRANCHES = {"子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥"};

    @Override
    public BaziResult calculate(BirthInfo birthInfo) {
        int year = birthInfo.birthday().getYear();
        int month = birthInfo.birthday().getMonthValue();
        int day = birthInfo.birthday().getDayOfMonth();
        int hour = birthInfo.birthHour();

        List<String> yearPillar = pillar(year);
        List<String> monthPillar = pillar(year + month);
        List<String> dayPillar = pillar(year + month + day);
        List<String> hourPillar = pillar(hour + day);

        return new BaziResult(
                yearPillar,
                monthPillar,
                dayPillar,
                hourPillar,
                Map.of(
                        "engine", "placeholder-v1",
                        "gender", birthInfo.gender(),
                        "location", birthInfo.province() + birthInfo.city()
                )
        );
    }

    private List<String> pillar(int seed) {
        int normalized = Math.floorMod(seed, 60);
        return List.of(STEMS[normalized % 10], BRANCHES[normalized % 12]);
    }
}
