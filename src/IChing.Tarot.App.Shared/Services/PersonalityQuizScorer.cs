using IChing.Tarot.App.Models;



namespace IChing.Tarot.App.Services;



public static class PersonalityQuizScorer

{

    public static PersonalityQuizResult Score(

        PersonalityQuizDefinition quiz,

        IReadOnlyDictionary<int, int> answers)

    {

        var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < quiz.Questions.Count; i++)

        {

            if (!answers.TryGetValue(i, out var optionIndex))

            {

                continue;

            }



            var q = quiz.Questions[i];

            if (optionIndex < 0 || optionIndex >= q.Options.Count)

            {

                continue;

            }



            foreach (var (key, value) in q.Options[optionIndex].Scores)

            {

                totals[key] = totals.GetValueOrDefault(key) + value;

            }

        }



        return quiz.Scoring switch

        {

            "mbti16" => ScoreMbti16(quiz.Scoring, totals),

            "enneagram9" => ScoreEnneagram9(quiz.Scoring, totals),

            "holland" => ScoreHolland(quiz.Scoring, totals),

            _ => throw new NotSupportedException($"未知计分：{quiz.Scoring}")

        };

    }



    private static PersonalityQuizResult ScoreMbti16(string scoring, Dictionary<string, int> totals)

    {

        static int v(Dictionary<string, int> t, string a, string b) =>

            t.GetValueOrDefault(a) - t.GetValueOrDefault(b);



        var type = string.Concat(

            v(totals, "E", "I") >= 0 ? "E" : "I",

            v(totals, "S", "N") >= 0 ? "S" : "N",

            v(totals, "T", "F") >= 0 ? "T" : "F",

            v(totals, "J", "P") >= 0 ? "J" : "P");



        var copy = PersonalityTypeCopy.Mbti16(type);

        return PersonalityQuizReportBuilder.Build(

            scoring,

            type,

            copy.Title,

            copy.Summary,

            copy.Detail,

            totals);

    }



    private static PersonalityQuizResult ScoreEnneagram9(string scoring, Dictionary<string, int> totals)

    {

        var top = Enumerable.Range(1, 9)

            .Select(n => (Type: n, Score: totals.GetValueOrDefault(n.ToString())))

            .OrderByDescending(x => x.Score)

            .First();



        var code = top.Type.ToString();

        var copy = PersonalityTypeCopy.Enneagram(top.Type);

        return PersonalityQuizReportBuilder.Build(

            scoring,

            code,

            copy.Title,

            copy.Summary,

            copy.Detail,

            totals);

    }



    private static PersonalityQuizResult ScoreHolland(string scoring, Dictionary<string, int> totals)

    {

        var order = new[] { "R", "I", "A", "S", "E", "C" };

        var ranked = order

            .Select(k => (Key: k, Score: totals.GetValueOrDefault(k)))

            .OrderByDescending(x => x.Score)

            .ToList();



        var code = string.Concat(ranked.Take(3).Select(x => x.Key));

        var copy = PersonalityTypeCopy.Holland(code, ranked);

        return PersonalityQuizReportBuilder.Build(

            scoring,

            code,

            copy.Title,

            copy.Summary,

            copy.Detail,

            totals);

    }

}


