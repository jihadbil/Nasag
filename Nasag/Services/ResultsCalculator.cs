using System.Collections.Generic;
using System.Linq;

namespace Nasag.Services;

public sealed class ResultsCalculator : IResultsCalculator
{
    // حدود التصنيف (نسبة مئوية) — قابلة للتعديل لاحقاً.
    private static readonly decimal ExcellentThreshold = 90m;
    private static readonly decimal VeryGoodThreshold = 80m;
    private static readonly decimal GoodThreshold = 70m;
    private static readonly decimal AcceptableThreshold = 50m;

    public StudentResultSummary Compute(StudentMarksInput input)
    {
        var subjectResults = new List<SubjectResult>(input.Subjects.Count);
        var failed = new List<string>();
        var missing = new List<string>();

        decimal total = 0m;
        decimal maxTotal = 0m;
        decimal examinedMax = 0m;

        foreach (var subject in input.Subjects)
        {
            maxTotal += subject.MaxMark;

            decimal weightedSum = 0m;
            decimal weightSum = 0m;
            foreach (var exam in subject.Exams)
            {
                if (exam.Value is null) continue;
                weightedSum += exam.Value.Value * exam.Weight;
                weightSum += exam.Weight;
            }

            if (weightSum == 0m)
            {
                subjectResults.Add(new SubjectResult(
                    subject.SubjectId,
                    subject.SubjectNameAr,
                    subject.MaxMark,
                    subject.PassMark,
                    Score: null,
                    IsAbsent: true,
                    IsPassed: false));
                missing.Add(subject.SubjectNameAr);
                continue;
            }

            // المادة لها درجات مُدخَلة — تُحتسب ضمن المواد المُمتحَنة.
            examinedMax += subject.MaxMark;

            var score = weightedSum / weightSum;
            var clamped = score < 0m ? 0m : score > subject.MaxMark ? subject.MaxMark : score;
            var passed = clamped >= subject.PassMark;
            if (!passed) failed.Add(subject.SubjectNameAr);

            total += clamped;
            subjectResults.Add(new SubjectResult(
                subject.SubjectId,
                subject.SubjectNameAr,
                subject.MaxMark,
                subject.PassMark,
                Score: clamped,
                IsAbsent: false,
                IsPassed: passed));
        }

        var percentage = examinedMax > 0m ? (total / examinedMax) * 100m : 0m;

        ResultGrade grade;
        bool isPassed;

        if (input.Subjects.Count == 0 || examinedMax == 0m)
        {
            grade = ResultGrade.Pending;
            isPassed = false;
        }
        else if (failed.Count > 0)
        {
            // الرسوب أولوية على النقص: الطالب راسب حتى لو هناك مواد غير مُدخَلة.
            grade = ResultGrade.Failed;
            isPassed = false;
        }
        else if (missing.Count > 0)
        {
            grade = ResultGrade.Pending;
            isPassed = false;
        }
        else
        {
            grade = ComputeGrade(percentage);
            isPassed = true;
        }

        return new StudentResultSummary(
            input.StudentId,
            input.StudentNumber,
            input.FullName,
            total,
            maxTotal,
            examinedMax,
            percentage,
            grade,
            isPassed,
            subjectResults,
            failed,
            missing);
    }

    // تُستدعى فقط عندما لا توجد مواد راسبة ولا ناقصة، لذا Failed لا يُرجع من هنا.
    private static ResultGrade ComputeGrade(decimal percentage)
    {
        if (percentage >= ExcellentThreshold) return ResultGrade.Excellent;
        if (percentage >= VeryGoodThreshold) return ResultGrade.VeryGood;
        if (percentage >= GoodThreshold) return ResultGrade.Good;
        if (percentage >= AcceptableThreshold) return ResultGrade.Acceptable;
        // إن وصلنا هنا فمعدل الطالب < 50% بدون أي failure — وضع نظرياً غير ممكن
        // لأن PassMark=50% لكل مادة سيُضمن وجود failure. نُرجع Acceptable كأمان.
        return ResultGrade.Acceptable;
    }
}
