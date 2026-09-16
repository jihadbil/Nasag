using System.Collections.Generic;

namespace Nasag.Services;

public interface IResultsCalculator
{
    StudentResultSummary Compute(StudentMarksInput input);
}

public sealed record SubjectMarksInput(
    int SubjectId,
    string SubjectNameAr,
    decimal MaxMark,
    decimal PassMark,
    IReadOnlyList<ExamMarkInput> Exams);

public sealed record ExamMarkInput(int ExamId, decimal Weight, decimal? Value);

public sealed record StudentMarksInput(
    int StudentId,
    string StudentNumber,
    string FullName,
    IReadOnlyList<SubjectMarksInput> Subjects);

public sealed record SubjectResult(
    int SubjectId,
    string SubjectNameAr,
    decimal MaxMark,
    decimal PassMark,
    decimal? Score,
    bool IsAbsent,
    bool IsPassed);

public enum ResultGrade
{
    Excellent,   // ممتاز
    VeryGood,    // جيد جداً
    Good,        // جيد
    Acceptable,  // مقبول
    Pending,     // غير مكتمل (مادة أو أكثر لم تُدخَل دون رسوب)
    Failed       // راسب
}

// IsPending = (Grade == ResultGrade.Pending). يعتمد الـ ViewModel على المقارنة المباشرة بالـ enum.
public sealed record StudentResultSummary(
    int StudentId,
    string StudentNumber,
    string FullName,
    decimal Total,
    decimal MaxTotal,
    decimal ExaminedMax,
    decimal Percentage,
    ResultGrade Grade,
    bool IsPassed,
    IReadOnlyList<SubjectResult> Subjects,
    IReadOnlyList<string> FailedSubjects,
    IReadOnlyList<string> MissingSubjects);
