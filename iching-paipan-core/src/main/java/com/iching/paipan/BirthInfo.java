package com.iching.paipan;

import java.time.LocalDate;

public record BirthInfo(
        LocalDate birthday,
        int birthHour,
        int gender,
        String province,
        String city
) {
}
