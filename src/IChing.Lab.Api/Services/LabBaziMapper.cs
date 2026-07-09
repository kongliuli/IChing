using IChing.Lab.Api.Contracts;
using IChing.Lab.Core.Bazi;

namespace IChing.Lab.Api.Services;

public static class LabBaziMapper
{
    public static BaziInput ToInput(BaziRequest req) =>
        new(req.Year, req.Month, req.Day, req.Hour, req.Minute, req.Second,
            req.Longitude, req.City, req.Gender, req.Sect,
            req.FlowYear, req.FlowMonth, req.FlowCalendarMonth, req.FlowDay);

    public static BaziInput ToInput(BaziReadRequest req) =>
        new(req.Year, req.Month, req.Day, req.Hour, req.Minute, req.Second,
            req.Longitude, req.City, req.Gender, req.Sect,
            req.FlowYear, req.FlowMonth, req.FlowCalendarMonth, req.FlowDay);

    public static BaziInput ToInput(BaziInterpretRequest req) =>
        new(req.Year, req.Month, req.Day, req.Hour, req.Minute, req.Second,
            req.Longitude, req.City, req.Gender, req.Sect,
            req.FlowYear, req.FlowMonth, req.FlowCalendarMonth, req.FlowDay);
}
