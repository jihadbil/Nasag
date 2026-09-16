using System;

namespace Nasag.Licensing.Trial;

/// <summary>
/// حالة فترة التجربة (30 يوماً) — مخزَّنة بصيغة DPAPI + مرآة سجل.
/// </summary>
public sealed record TrialState(DateTime TrialStartUtc, DateTime LastSeenUtc, string Hmac);
