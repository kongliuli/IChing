package com.iching.paipan;

import org.junit.jupiter.api.Test;

import java.time.LocalDate;

import static org.junit.jupiter.api.Assertions.assertEquals;

class PlaceholderBaziCalculatorTest {

    private final BaziCalculator calculator = new PlaceholderBaziCalculator();

    @Test
    void calculateIsDeterministic() {
        BirthInfo birth = new BirthInfo(LocalDate.of(1990, 5, 20), 10, 1, "广东", "深圳");
        BaziResult first = calculator.calculate(birth);
        BaziResult second = calculator.calculate(birth);
        assertEquals(first, second);
        assertEquals("placeholder-v1", first.meta().get("engine"));
    }
}
