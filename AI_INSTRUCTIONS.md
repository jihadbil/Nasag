# AI Instructions for Nasaq School Management

> ملف تعليمات دائم لأي وكيل AI يعمل على مشروع **نَسَق لإدارة المدارس**.
> هذا الملف يُقرأ **قبل** أي عمل، وقبل `Agent.md`، وقبل لمس أي كود.

---

## Purpose

هذا المشروع هو برنامج WPF عربي لإدارة المدارس باسم **نَسَق**.
- منصة: Windows Desktop
- لغة: C# / .NET 8 / WPF
- اللغة الوحيدة في الواجهة: العربية
- الاتجاه: RTL فقط
- قاعدة البيانات: SQL Server
- المرجع البصري الإلزامي: مجلد `UI/`

---

## Mandatory Startup Routine

عند بدء أي وكيل جديد العمل على المشروع، يجب عليه بالترتيب:

1. **قراءة هذا الملف بالكامل** (`AI_INSTRUCTIONS.md`).
2. **قراءة `Agent.md` بالكامل** — هو ذاكرة المشروع وخطة العمل الرسمية.
3. فحص قسم **8. Current Progress** في `Agent.md` لتحديد آخر مرحلة مكتملة.
4. اختيار المرحلة الحالية أو التالية فقط للعمل عليها.
5. عدم تغيير القرارات السابقة (قسم 9 Decisions Log) إلا عند خطأ واضح وموثّق.
6. تنفيذ العمل ضمن المرحلة الحالية فقط.
7. تحديث `Agent.md` (الأقسام 8، 9، 10 حسب الحاجة) بعد كل إنجاز.
8. كتابة رسالة اختتام مختصرة وفق قاعدة Completion Message أدناه.

> **لا تبدأ أي عمل قبل إكمال الخطوات 1-3 من هذه الروتين.**

---

## Development Rules

### قواعد عامة
- لا تبدأ أي عمل بدون قراءة `Agent.md`.
- لا تحذف ملفات أو تعدّل بنية كبيرة بدون سبب واضح ومسجّل.
- لا تستخدم صور `UI/` كواجهات ثابتة (ImageBrush، Image كخلفية للنافذة، إلخ). هي مرجع بصري فقط.
- كل الواجهات يجب أن تكون XAML حقيقي مبني من Controls حقيقية.
- كل الواجهات باللغة العربية فقط — لا labels إنجليزية ظاهرة للمستخدم.
- كل الواجهات RTL (`FlowDirection="RightToLeft"`).
- استخدم **Tajawal** كخط أساسي.
- حافظ على ألوان وهوية نَسَق (الموثقة في القسم 3 من `Agent.md`).
- لا تضف وظائف غير مذكورة في `Agent.md`.
- لا تضف تعقيداً غير ضروري — لا abstractions تخمينية.
- ركّز على برنامج عملي، سريع، سهل الاستخدام.
- تجنّب التصميم القديم المشابه لـ Windows Forms (لا حدود مربعة صلبة، لا ألوان نظام افتراضية، لا خطوط Tahoma/Segoe افتراضية).
- استخدم MVVM دائماً — لا منطق أعمال في code-behind.
- افصل الواجهة عن منطق العمل عبر ViewModels و Services.
- استخدم Repositories للوصول إلى البيانات؛ لا تستدعِ DbContext مباشرة من ViewModel.
- **لا تكرر Styles** — أنشئ Resource مشترك في `/Themes`.
- اختبر Build بعد كل تعديل جوهري.
- أصلح الأخطاء من جذورها وليس بشكل مؤقت — لا workarounds مخفية.

### ممنوعات صارمة
- ❌ لا تستخدم Windows Forms أو UWP أو MAUI.
- ❌ لا تضف SQLite — قاعدة البيانات SQL Server فقط (ما لم يطلب المستخدم صراحةً غير ذلك).
- ❌ لا تضف ميزات LMS، تطبيقات هواتف، باصات، مكتبة، كافتيريا، دردشة، دفع إلكتروني، محاسبة كاملة، أو AI داخلي.
- ❌ لا تكتب نصوصاً إنجليزية في الواجهة.
- ❌ لا تستخدم أيقونات Wingdings أو خطوط رمزية للأيقونات — استخدم Geometry أو مكتبة أيقونات حديثة.
- ❌ لا تكشف كلمات المرور بشكل صريح — استخدم Hashing دائماً.

---

## UI Rules

### التخطيط
- **Right Sidebar ثابتة** على اليمين بخلفية Navy.
- **Top Bar ثابت** أعلى الشاشة بخلفية بيضاء.
- **Main Content Area** في الوسط بخلفية فاتحة (off-white #F5F7FB).

### العناصر
- بطاقات بيضاء (`#FFFFFF`) بحواف مدورة (~12px) وظل ناعم.
- خلفية القائمة الجانبية Navy (`#0E2A47`).
- الأزرار الأساسية Teal (`#1FB5A8`) مع نص أبيض.
- الأزرار الثانوية بيضاء بإطار رمادي فاتح.
- ظلال ناعمة (Opacity 0.08-0.12، Blur 12-20).
- حواف مدورة (BorderRadius / CornerRadius = 6-12).
- DataGrid نظيف: لا خطوط شبكة سميكة، فاصل أفقي رفيع، رأس بخلفية فاتحة، أزرار إجراءات أيقونية صغيرة في عمود مخصص.
- شارات الحالة كـ Pills ملونة شفافة.

### الكتابة
- لغة عربية فقط في النص الظاهر.
- تنسيق RTL على كل النصوص.
- حجم الخط الأساسي 14px للنصوص، 16-18px للعناوين الفرعية، 20-24px للعناوين الرئيسية.
- اللون الأساسي للنص Navy داكن، الثانوي رمادي معتدل.
- الجداول يجب أن تكون عملية وقابلة للقراءة — مسافات كافية بين الصفوف.

### الأيقونات
- نمط outline / line-icons موحد (سُمك ~1.5).
- حجم 18-22px في القائمة الجانبية، 14-16px داخل الجداول والأزرار.
- مصدرها Geometry في `Themes/Icons.xaml` (تجنب صور PNG للأيقونات).

---

## Database Rules

- **SQL Server** هو قاعدة البيانات الأساسية.
- LocalDB في التطوير، Express/Standard في الإنتاج.
- لا تستخدم SQLite إلا إذا طلب المستخدم صراحةً.
- استخدم **EF Core 8** مع Migrations.
- لا تنشئ جداول غير ضرورية — التزم بقائمة القسم 5 في `Agent.md`.
- أسماء الجداول واضحة بالإنجليزية (PascalCase، جمع: Students, Sections, ...).
- استخدم علاقات صحيحة (Foreign Keys حقيقية، Navigation Properties).
- أضف **Seed Data** تجريبية واقعية تساعد على اختبار كل واجهة:
  - مدرسة النور الأهلية
  - السنة الدراسية 2025 - 2026
  - أسماء عربية واقعية للطلاب
  - صفوف ابتدائي/متوسط/ثانوي
  - شعب أ/ب/ج
  - مستخدم admin/admin123 (مدير النظام)
- كلمات المرور مُجزّأة (Hash) دائماً عبر BCrypt أو PBKDF2.
- التواريخ تُخزّن UTC في DB، تُعرض بالميلادي + الهجري عند الحاجة (Phase 13).

---

## Agent.md Update Rules

بعد إكمال أي مرحلة أو جزء مهم، يجب تحديث `Agent.md` بـ:

1. **القسم 8 (Current Progress):**
   - تغيير حالة المرحلة إلى `In Progress` أو `Completed`.
   - تحديث تواريخ Started/Completed.
   - إضافة Notes مختصرة (ماذا تم فعلاً، أي اختصارات أو تأجيلات).

2. **القسم 9 (Decisions Log):**
   - إضافة أي قرار جديد (مكتبة، اختصار، نمط) مع السبب.

3. **القسم 10 (Issues and Risks):**
   - إضافة أي مشكلة ظهرت أثناء التنفيذ مع التخفيف المعتمد.

4. **قائمة Tasks داخل المرحلة:**
   - وضع علامة `[x]` على المهام المكتملة.
   - إضافة Tasks فرعية إن ظهرت.

5. **إذا تغيّر هيكل المجلدات أو حزم NuGet، حدّث القسم 6.**

---

## Git Commit Rule

في نهاية كل جلسة عمل (turn) ينجز فيها الوكيل تغييرات فعلية على المشروع:

1. **يجب على الوكيل أن يطلب الإذن من المستخدم لعمل commit قبل التنفيذ.** لا commit تلقائي بدون سؤال.
2. عند الموافقة، يُنفَّذ الـ commit بإعدادات Git المحلية للمطوّر **كما هي** — أي بحساب المطوّر (`user.name` و`user.email` من `git config`).
3. **ممنوع** إضافة سطر `Co-Authored-By: Claude …` أو أي co-author آخر في رسالة الـ commit.
4. **ممنوع** تغيير `user.name` أو `user.email` أو أي إعداد Git في إعدادات النظام/المستودع.
5. رسالة الـ commit عربية مختصرة (1-2 سطر) تصف ما تم فعلاً في الجلسة.
6. لا تُمرَّر `--no-verify` أو `--no-gpg-sign` إلا إذا طلب المستخدم ذلك صراحةً.
7. إذا لم يكن المجلد مستودع Git بعد، اسأل المستخدم قبل تنفيذ `git init`.

> ملاحظة: في وقت كتابة هذا الملف لم يكن المجلد مستودع Git. أوّل commit يتطلب `git init` بإذن صريح من المستخدم.

---

## Global System Services (مطلوبة لكل شاشة)

هذه الخدمات تأسست في Phase 6 وأصبحت جزءاً دائماً من البنية. **لا تكتب catch لمنطق العمل بدون استدعائها**:

### `IErrorReporter` (نظام الأخطاء العام)
- كل `catch (Exception ex)` في ViewModel/Service يجب أن ينتهي بـ `_errors.Report("عنوان عربي", ex.Message, ex);`.
- لا تستخدم `MessageBox.Show` للأخطاء — استعمل ErrorReporter دائماً (يفتح نافذة `ErrorWindow` مع زر «نسخ كامل التفاصيل» وخلفية حمراء).
- AppDomain/Dispatcher/TaskScheduler الثلاثة موصولون بالفعل في `App.OnStartup` — لا تفصلهم.
- لا تُخفِ الخطأ بـ `catch { }` صامت أبداً.

### `IToastService` (إشعارات احترافية)
- بعد كل عملية ناجحة على قاعدة البيانات: `_toasts.Success("...", "...");`
- عند رفض تحقّق Validation: `_toasts.Warning("...", "...");`
- عند بدء عملية طويلة بدون LoadingOverlay: `_toasts.Info("...", "...");`
- لا تستخدم Toast كبديل عن ErrorReporter للأخطاء التقنية — Toast للأحداث الناعمة، ErrorReporter للأخطاء الفعلية.
- `ToastHost` موضوع في `MainShellView` فوق منطقة المحتوى تلقائياً — لا تضيفه يدوياً في كل صفحة.

### `IDialogService` (تأكيد/تنبيه RTL)
- استخدمه لكل عملية حذف/أرشفة/استعادة/إعادة تعيين: `await _dialogs.ConfirmAsync("عنوان", "رسالة")`.
- لا تنفّذ عملية مدمّرة بدون `Confirm` صريح.

---

## Page Layout Rules (مطلوبة لكل شاشة بيانات)

التخطيط القياسي لأي شاشة قائمة (Students, Classes, Subjects, Marks, Fees, Reports, Users, ...):

1. **لا سكرول على مستوى الصفحة.** كل الصفحة تظهر داخل النافذة بدون `ScrollViewer` خارجي.
2. **التخطيط الإلزامي بـ Grid عمودي 3 صفوف:**
   - `Auto`: ترويسة الصفحة (عنوان يميناً + معلومات إجمالية مختصرة) + شريط الأدوات الرئيسي (أزرار «تحديث»، «إضافة …»، «تصدير …») يساراً.
   - `Auto`: شريط الفلاتر/البحث (Card مع padding مضغوط).
   - `*`: بطاقة DataGrid تأخذ كل المساحة المتبقية مع pagination footer مدمج.
3. **DataGrid يحتوي السكرول الداخلي** عبر `ScrollViewer.HorizontalScrollBarVisibility="Auto"` و`ScrollViewer.VerticalScrollBarVisibility="Auto"` + `EnableRowVirtualization="True"` + `EnableColumnVirtualization="True"`.
4. **Pagination ثابت في أسفل بطاقة الـ DataGrid** بـ `RowDefinition Height="Auto"` خاص به، يحوي:
   - Label واضح: «الصفحة X من Y — إجمالي N».
   - زرّا «السابق» و«التالي» مع تعطيل تلقائي عند الحدود (`NotifyCanExecuteChanged`).
   - (اختياري) أزرار أرقام صفحات مرئية إذا كانت `TotalPages <= 10`.
5. **لا تضع بطاقات إحصائية كبيرة أعلى الصفحات** — اعرض الأرقام الإجمالية كنص مضغوط داخل ترويسة الصفحة (مثل: "إجمالي: 1,248 • نشطون: 1,220 • مؤرشفون: 28"). لوحة التحكم (Dashboard) فقط هي الاستثناء حيث البطاقات الإحصائية محتوى رئيسي.
6. **شاشات النماذج (Add/Edit):** زرا «حفظ» و«إلغاء» في **شريط أدوات ثابت أعلى الصفحة** (Card sticky)، وجسم النموذج هو الجزء الوحيد الذي يحتوي ScrollViewer داخلياً. لا تضع أزرار الحفظ في الأسفل بحيث تتطلب التمرير.
7. كل الأدوات بـ Padding مضغوط (`Padding="14,8"` للأزرار، `Padding="10,2"` للـ TextBox، `Padding="14,10"` لشريط الفلاتر) — التصميم احترافي مدمج لكن بدون تصغير حقيقي للقراءة.

---

## Data Storage Rule (DB-only)

- **كل البيانات بما فيها الصور والملفات تُحفظ في قاعدة البيانات** كأعمدة `varbinary(max)` (مثل `Student.PhotoBytes`). لا توجد ملفات على القرص المحلي.
- لا تستخدم `LocalAppData`/`Pictures`/`AppContext.BaseDirectory` لتخزين بيانات قابلة للنسخ الاحتياطي.
- استخدم `IFileService.PickImage()` لاختيار الملف، ثم `IFileService.ReadAllBytesAsync(path)` لتحويله إلى `byte[]` لحفظه في DB.
- العرض في الواجهة عبر `BytesToImageSourceConverter` (BitmapImage مجمَّد من MemoryStream).
- لا توجد عمليات حذف ملفات محلية لأنها لا تُكتب أصلاً.

---

## Async / Background Work Rule

كل عملية قد تستغرق أكثر من ~150ms (أي عملية قاعدة بيانات، اتصال شبكي، قراءة/كتابة ملف، تصدير تقرير، نسخة احتياطية…) **يجب** أن تكون:

1. **غير حاجبة (Async/Non-blocking):** استخدم `async`/`await` في الـ ViewModel، ولا تستدعِ `.Result` أو `.Wait()` على Task.
2. **مع مؤشر تحميل (Loading Indicator):** كل عملية تظهر فيها حالة "جاري التحميل…" — إما:
   - `LoadingOverlay` فوق المنطقة المتأثرة (شبه شفاف + سبيرنر + نص).
   - أو حالة busy على الزر نفسه (تعطيل + سبيرنر صغير داخل الزر).
   - أو سبيرنر سطري داخل الجدول/البطاقة عند التحميل الأولي.
3. **مع إلغاء حيث ينطبق:** عمليات بحث / تصدير طويلة يجب أن تكون قابلة للإلغاء عبر `CancellationToken`.
4. **مع حالة Empty / Error واضحة:** الجداول والبطاقات يجب أن تعرض حالة فارغة عندما لا توجد بيانات، وحالة خطأ مع زر إعادة المحاولة عند الفشل.

### معالجة انقطاع الاتصال بـ SQL Server

- يجب أن يكشف البرنامج فقدان الاتصال بقاعدة البيانات (SqlException، timeout) **دون أن ينهار**.
- يُعرض **شريط تنبيه (ConnectionStatusBanner)** بلون Danger أعلى المحتوى يحوي: نص الخطأ المختصر بالعربية + زر "إعادة المحاولة".
- العمليات الجديدة في حالة الانقطاع تُمنع وتُظهر نفس الشريط.
- بعد نجاح إعادة الاتصال يختفي الشريط تلقائياً ويُستأنف العمل.
- استخدم `EnableRetryOnFailure` في إعداد EF Core كآلية إعادة محاولة شفافة على المستوى الأدنى.

### قواعد البيانات والـ Migrations

- **Migrations تُطبَّق تلقائياً عند تشغيل التطبيق** عبر `Database.MigrateAsync()` في طبقة البدء — بحيث تُلتقط أي Migration جديدة مستقبلية تلقائياً دون كود مخصّص لكل واحدة.
- منطق التطبيق: (1) محاولة الاتصال → (2) قائمة `GetPendingMigrationsAsync()` → (3) إن وُجدت، تنفيذ `MigrateAsync()` مع عرض تقدّم → (4) Seed لو القاعدة فارغة → (5) فتح الواجهة الرئيسية.
- لا تستخدم `EnsureCreated()` أبداً — فهي تتجاوز Migrations.
- كل تغيير على المخطط يمرّ عبر Migration جديد (`dotnet ef migrations add …`) — لا تعديل يدوي على الجداول.
- لا تحذف Migration بعد نشرها — أنشئ Migration جديد عوضاً عن ذلك.

### Transactions مع EnableRetryOnFailure (مهمّ جداً)

- المشروع يفعّل `EnableRetryOnFailure` لمرونة الشبكة. هذا يمنع `BeginTransactionAsync` المباشر بسبب: «`SqlServerRetryingExecutionStrategy does not support user-initiated transactions`».
- **القاعدة:** أي عملية تحتاج Transaction يدوي **يجب** أن تُغلَّف داخل ExecutionStrategy:

```csharp
var strategy = ctx.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
    // ... SaveChangesAsync calls ...
    await tx.CommitAsync(ct).ConfigureAwait(false);
});
```

- إذا كانت العملية SaveChanges واحد فقط، EF Core يدير transaction ضمنياً ولا تحتاج للنمط أعلاه.

---

## Phase Execution Rule — مرحلة واحدة كاملة (إلزامي)

كل مرحلة في `Agent.md` تُنفَّذ كوحدة واحدة كاملة. **ممنوع** تجزئتها إلى Sub-phases (مثل 10.1، 10.2، 9.1، 6.1، 6.2، 6.3 …) في الـ commits أو في `Agent.md` أو في رسائل الإنجاز.

**القاعدة:**
- قبل إعلان أي مرحلة كمكتملة، نفّذها بكامل متطلباتها الوظيفية + UX + التكامل + الصلاحيات + الـ Seeder + الطباعة + تدقيق نهائي. لا تترك ثغرات «نُكملها لاحقاً في 10.1».
- اعتمد دورة متعددة الوكلاء في نفس الجلسة عند اللزوم: (مراجعة بالتوازي) → (تخطيط) → (تنفيذ بالتوازي بفصل واضح للملفات) → (تدقيق نهائي مستقل) → (إصلاحات Blockers) → (Build 0/0) → (commit واحد للمرحلة بأكملها).
- في `Agent.md` (القسم 8 Current Progress)، كل مرحلة تظهر **بإدخال واحد فقط** بعنوان «Phase N — …». إن استدعى الأمر دورة polish لاحقة بناءً على ملاحظات المستخدم، **ادمج** التحسينات في الإدخال نفسه بدل إنشاء «Phase N.1».
- رسائل الـ commit للمراحل يجب أن تستخدم «Phase N» فقط (بالعربية: «المرحلة N»). **ممنوع** «Phase N.1» أو «Phase N — Polish» كرسالة منفصلة.
- إذا اكتشفت بعد commit مرحلة أن هناك Bug أو Polish ناقص، اعمل commit إصلاح بعنوان وصفي للمحتوى (مثل «إصلاح ذرّية رقم السند في الرسوم») دون رقم مرحلة فرعي.

**لماذا:** يحفظ تاريخ Git نظيفاً ومقروءاً، يجبر على إكمال المرحلة فعلياً قبل اعتبارها مكتملة، ويمنع تراكم Sub-phases تشوّش على الخطة الأصلية.

---

## Completion Message Rule

عند الانتهاء من جلسة عمل، **لا تكتب شرحاً طويلاً**. اكتب فقط:

```
ما تم إنجازه:
- ...
- ...

حالة Build: ✅ ناجح / ❌ فشل (السبب)

الملفات المهمة التي تغيّرت:
- path/to/file
- ...

المرحلة التالية المقترحة: Phase X — ...
```

---

## UI Component Standards (Phase 6+ — إلزامية لكل الشاشات)

هذه المعايير اعتُمدت رسمياً في Phase 6 وأصبحت Templates ملزمة لكل شاشات قادمة (الصفوف والشعب، الحضور والغياب، المواد، الدرجات، الرسوم، التقارير، المستخدمون، إلخ). أي مطور / وكيل AI يبني شاشة جديدة **يجب** أن يستخدم هذه المكونات والقواعد كما هي.

### 1. محاذاة العناوين (Page Headers) — **FlowDirection Swap Pattern (إلزامي)**

`HorizontalAlignment="Right"` داخل حاوية `FlowDirection="RightToLeft"` يُعكس بصرياً إلى اليسار في بعض المخططات (خاصة مع `ColumnDefinition Width="*"`). لذلك **لا تعتمد** على `HorizontalAlignment` وحده لتثبيت العنوان على اليمين.

**القاعدة الإلزامية:** غلِّف صف الترويسة بـ `FlowDirection="LeftToRight"`، ضع `Grid` بعمودين، الترويسة في العمود الأيمن (`Auto`)، الإجراءات في العمود الأيسر، ثم أعد الـ FlowDirection إلى `RightToLeft` داخل StackPanel العنوان فقط لضمان تشكيل النص العربي. مثال قياسي:

```xml
<Grid Grid.Row="0" Margin="0,0,0,14" FlowDirection="LeftToRight">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>
    <!-- Title pinned to the actual visual right -->
    <StackPanel Grid.Column="1"
                FlowDirection="RightToLeft"
                HorizontalAlignment="Right"
                VerticalAlignment="Center">
        <TextBlock Text="{Binding TitleAr}"
                   Style="{StaticResource SectionTitle}"
                   TextAlignment="Right"
                   HorizontalAlignment="Right" />
        <TextBlock Text="{Binding SubtitleAr}"
                   Style="{StaticResource SectionSubtitle}"
                   TextAlignment="Right"
                   HorizontalAlignment="Right"
                   Margin="0,2,0,0" />
    </StackPanel>
</Grid>
```

عندما يكون هناك زر يميناً (لوحة التحكم Refresh)، أضف عموداً ثالثاً (`Auto`) للزر في `Grid.Column="0"`، واحفظ StackPanel العنوان في `Grid.Column="2"`.

- استخدم `SectionTitle` و`SectionSubtitle` من `Themes/Cards.xaml`.
- **لا تستخدم `controls:SectionHeader`** لشاشات البيانات (هذا Placeholder قديم).

### 2. شريط الأدوات (Toolbar)
- صف واحد فقط (Row) داخل `CardBorder` بـ `Padding="14,10"`.
- **زر الإضافة الرئيسي (Add) لا يوضع في الـ Toolbar** — هو دائماً FAB في الزاوية السفلية اليمنى (انظر القسم 8 ب).
- ترتيب الأعمدة من اليمين بصرياً إلى اليسار:
  1. الفلاتر الـ Searchable (الصف، الشعبة، الحالة، …).
  2. حجم الصفحة (PageSize).
  3. مربع البحث (يأخذ `Width="*"`).
  4. زر تحديث البيانات (`IconRefresh`).
  5. زر مسح الفلاتر (`IconFilterClear`) — **لازم** يستخدم أيقونة مختلفة عن التحديث.
  6. أزرار التصدير/الاستيراد إن وُجدت.
- **لا تستخدم نفس الأيقونة لزرّين** في نفس الشريط.
- **لا تنشئ صفّ Toolbar ثاني** — كل الفلاتر في نفس الصف.

### 3. Comboboxes / Dropdowns
- **استخدم `Nasag.Controls.SearchableComboBox` فقط** لأي قائمة منسدلة في الفلاتر أو النماذج. الخصائص:
  - `ItemsSource`, `SelectedItem` (TwoWay), `DisplayMemberPath`, `Placeholder`.
  - قابلة للبحث (Searchable)، تقترح فورياً، اختيارية (يمكن إفراغها بزر ×)، تُغلق عند فقد التركيز، تدعم Arrow/Enter/Esc.
- **ممنوع** استخدام `ComboBox` الافتراضي إلا للحقول الـ Enum الثابتة (مثل الجنس، صلة القرابة)؛ وحتى عند ذلك، الـ `Theme` يرسم ComboBox بشكل احترافي.
- لا تجبر اختياراً مبدئياً (مثل "كل الحالات") — السماح بـ `null` كـ "لا فلتر" أفضل تجربة مستخدم.

### 4. شبكة عرض البيانات (DataGrid)
- الـ Theme (`Themes/DataGrid.xaml`) يضبط:
  - `GridLinesVisibility="All"` — خطوط شبكة أفقية وعمودية كاملة.
  - `HorizontalContentAlignment="Center"` لكل خلية ورأس.
  - حدود قوية للحاوية (`BorderBrush=BorderStrongBrush`).
  - Hover خفيف ((TealSoft soft)) و Selected سوفت تيل.
- في كل عمود، **اضبط** `DataGridTextColumn.ElementStyle` ليجعل النص `HorizontalAlignment="Center"`. للأعمدة المخصصة، الـ `DataTemplate` يستخدم `HorizontalAlignment="Center"`.
- **الإجراءات في عمود مخصص (`إجراءات`)** بترتيب أيقونات: تعديل → أرشفة (أو استعادة عند الحالة المؤرشفة) → حذف.
- فعّل `MouseDoubleClick` على الـ DataGrid لفتح Editor الصف.
- **لا تضع بطاقات إحصائية أعلى الصفحة** — أدمج الإحصائيات في `SubtitleAr` كنص مضغوط (الإستثناء: لوحة التحكم Dashboard).

### 5. الترقيم (Pagination)
- Footer ثابت أسفل بطاقة الـ DataGrid يحتوي:
  - Label: "الصفحة X من Y — إجمالي N".
  - زر السابق + ComboBox للقفز إلى صفحة (Editable + قائمة بكل أرقام الصفحات) + زر التالي.
  - عند الضغط على Enter داخل الـ ComboBox، انتقل للصفحة المكتوبة.
- استخدم `PageNumbers` `ObservableCollection<int>` في الـ ViewModel و`JumpToPageCommand`.

### 6. Toast Notifications
- موقع: **الزاوية اليسرى من الشاشة** (`ToastHost` بـ `FlowDirection=LeftToRight` + `HorizontalAlignment="Left"` داخل Stretch).
- يحوي 4 حالات: Success / Error / Warning / Info — كل حالة بحدّ ملوّن من اليسار + Icon Bubble + Title (Bold) + Message (Muted) + Close Button.
- يُسحب تلقائياً بعد 4 ثوانٍ.
- استخدم `_toasts.Success/Warning/Info/Error(title, message)` بعد كل عملية ناجحة على القاعدة أو رفض Validation.

### 7. MessageBoxes / Dialogs / Warnings
- **ممنوع منعاً باتاً** استخدام `MessageBox.Show` من Windows. استخدم `IDialogService`:
  - `ConfirmAsync(...)` للتأكيدات العادية.
  - `ConfirmDestructiveAsync(...)` للحذف/إلغاء التراجع — يظهر زر أحمر.
  - `ShowInfoAsync / ShowSuccessAsync / ShowWarningAsync / ShowErrorAsync`.
- الـ Dialog تحت الغطاء هو `Nasag.Views.Common.NasaqDialog`: نافذة بحواف مدورة، خط Tajawal، RTL، أيقونة ملوّنة في الرأس، أزرار Primary/Secondary بـ DangerButton عند الحذف.
- للأخطاء التقنية الفعلية، استخدم `IErrorReporter` (نافذة `ErrorWindow` مع زر «نسخ التفاصيل»).

### 8. Buttons

#### أ) أنماط الأزرار العامة
- `PrimaryButton`: الأزرار الإيجابية داخل النماذج (حفظ، تنفيذ).
- `SecondaryButton`: الإجراءات الثانوية (تحديث، تصدير، استيراد، رجوع).
- `GhostButton`: الإجراءات الخفيفة (إلغاء، إغلاق، رابط جانبي).
- `DangerButton`: الحذف القوي (يُستخدم تلقائياً داخل `ConfirmDestructiveAsync`).
- `IconButton` / `RowActionButton`: أيقونات صغيرة في الجداول وشريط العنوان.
- `BubbleButton`: زر CTA تيل مدوّر باعتدال (`RadiusMd` = 10) بدون ظل توهّجي. يُستخدم لإجراءات الإضافة الرئيسية على رؤوس الكروت (مثلاً «إضافة شعبة» في بطاقة الشعب أو «إضافة صف» في فوتر قائمة الصفوف). **لا** يصلح بديلاً للـ FAB ولا يُستخدم بـ `CornerRadius=999` (pill ممنوع لتجنّب البصمة البصرية المبالغ فيها).

#### ب) **FabButton — الـ CTA الوحيد لشاشات القائمة (إلزامي)**
زر **دائري كامل 60×60** (CornerRadius=999) بخلفية Teal + Drop shadow Teal soft، يحوي أيقونة بيضاء فقط بدون نص. **لا يوضع في الـ Toolbar** — يوضع كـ overlay فوق محتوى الصفحة، مثبَّتاً في **الزاوية السفلية اليمنى** بمسافة 28px من الحافة.

**النمط القياسي:**
```xml
<Grid FlowDirection="LeftToRight"
      Visibility="{Binding ShowList, Converter={StaticResource BoolToVisibility}}">
    <Button Style="{StaticResource FabButton}"
            Command="{Binding AddStudentCommand}"
            HorizontalAlignment="Right"
            VerticalAlignment="Bottom"
            Margin="0,0,28,28"
            ToolTip="إضافة طالب (Ctrl+N)">
        <Path Data="{StaticResource IconAdd}"
              Stroke="White"
              StrokeThickness="2.6"
              StrokeStartLineCap="Round"
              StrokeEndLineCap="Round"
              Fill="Transparent"
              Stretch="Uniform"
              Width="24" Height="24" />
    </Button>
</Grid>
```

ملاحظات:
- **FlowDirection=LeftToRight** على الـ Grid يضمن أن `HorizontalAlignment="Right"` تعني الحافة البصرية اليمنى الفعلية.
- أخفِ الـ FAB في وضع المحرر (`ShowEditor`) — لا يجب أن يظهر فوق نموذج التعديل.
- استخدم Z-Order طبيعي (آخر عنصر داخل الـ Grid الخارجي قبل LoadingOverlay) لضمان ظهوره فوق كل شيء.

### 9. Photo / Image Upload
- استخدم `IFileService.PickImage()` (يقبل Owner اختياري).
- منطقة الصورة كاملة قابلة للنقر (Button مع Template مخصص) — تظهر فارغة بأيقونة كاميرا + نص "اضغط لاختيار صورة" + قائمة الصيغ المدعومة.
- الصورة المحفوظة في `byte[]` PhotoBytes، تُعرض عبر `BytesToImageSourceConverter`.

### 10. Keyboard Shortcuts
- **شاشات القوائم** (Students, Classes, …): فعّل عبر `UserControl.InputBindings`:
  - `Ctrl+N` → إضافة عنصر جديد.
  - `F5` → تحديث.
  - `Delete` → حذف الصف المحدد.
  - `Ctrl+F` → التركيز على مربع البحث (يُحقن في code-behind).
- **شاشات النماذج (Editors)**: فعّل:
  - `Ctrl+S` → حفظ.
  - `Escape` → إلغاء/رجوع.

### 11. Import / Export Patterns
- **التصدير:** `IExcelService.ExportStudentsAsync(path, rows)` — ينتج ملف `.xlsx` احترافي بأعمدة عربية، رأس مجمَّد، Banded Rows، حدود رفيعة، Auto-fit. الأعمدة 20 عمود قياسي للطلاب.
- **الاستيراد:** عبر `StudentImportWizard` (نافذة Modal بـ 4 خطوات: اختيار الملف → مراجعة الصفوف الصحيحة والخاطئة → اختيار طريقة (إضافة / حذف ثم استيراد) → نتيجة). كل خطوة تظهر Stepper مرئي. يجب تقديم زر "تنزيل قالب فارغ" دائماً.

### 12. User Preferences
- إعدادات المستخدم (RememberMe، حجم الصفحة، ترتيب الطلاب، …) تُحفظ في `%LOCALAPPDATA%\Nasaq\prefs.json` عبر `IUserPreferencesService`.
- **ليست بيانات قابلة للنسخ الاحتياطي** — هي تفضيلات Per-Machine، لذلك مستثناة من قاعدة "كل البيانات في DB".
- لا تضف خاصية جديدة في `UserPreferences` بدون توثيقها هنا.

### 13. منع تنشيط Setter-Cascades في المنشئ (CRITICAL)

أي PageViewModel يحتوي على خصائص `[ObservableProperty]` بحقول `partial OnXxxChanged` تستدعي `ReloadAsync` أو عمليات DB، **يجب** أن يستخدم النمط التالي في المنشئ:

```csharp
private bool _isInitializing = true;

public StudentsViewModel(...)
{
    // 1) Assign backing fields DIRECTLY (skip property setters so the
    //    OnXxxChanged source-generated callbacks don't fire).
    _selectedStatus = StatusOptions[0];
    _pageSize = prefs.Current.StudentsPageSize;

    _isInitializing = false;
}

partial void OnSelectedStatusChanged(StudentStatusFilter value)
{
    if (_isInitializing) return;   // belt-and-suspenders
    ResetPageAndReload();
}
```

**لماذا:** بدون هذا النمط، المنشئ يُطلق 2-3 `ReloadAsync` متداخلة قبل ظهور الصفحة، فتظهر للمستخدم كأنها مُجمَّدة لمدة 500ms-1s. أيضاً، Setter-cascades قد تتسبب في حالات سباق على `ObservableCollection<T>` (الـ DataGrid يربط معها) عبر الـ Dispatcher.

**أيضاً:** أي `[RelayCommand]` Load/Reload **يجب** أن يحتوي على re-entrance guard:

```csharp
private bool _reloadInFlight;
[RelayCommand]
public async Task ReloadAsync(CancellationToken ct = default)
{
    if (_reloadInFlight) return;
    _reloadInFlight = true;
    try { await ReloadCoreAsync(ct); }
    finally { _reloadInFlight = false; IsLoading = false; }
}
```

استخرج جسم العملية إلى `private async Task ReloadCoreAsync(...)` ليُستدعى Recursive (مثلاً عند تصحيح صفحة Pagination خارج النطاق) دون الاصطدام بـ guard.

### 14. النقاط المرجعية (Reference Files)
أي شاشة جديدة، انسخ النمط من هذه الملفات كـ Source of Truth:
- `Views/Pages/Students/StudentsView.xaml` — Page Layout كامل (Header + Toolbar + DataGrid + Pagination).
- `Views/Pages/Students/StudentEditorView.xaml` — Form Layout كامل (Action Bar + Cards + Photo Dropzone).
- `Views/Pages/Students/StudentImportWizard.xaml` — Multi-step Wizard.
- `Views/Common/NasaqDialog.xaml` — Custom Modal Dialog.
- `Controls/SearchableComboBox.xaml` — Combobox القياسي.
- `Themes/DataGrid.xaml` — أنماط الجدول.
- `Themes/Buttons.xaml` — أنماط الأزرار (BubbleButton, PrimaryButton, …).

---

## Conflict Resolution

في حال تعارض بين هذا الملف و`Agent.md`:
- **`AI_INSTRUCTIONS.md` يحدد القواعد العامة (هذا الملف).**
- **`Agent.md` يحدد الخطة والتقدم.**
- إذا تعارض البند، اتبع `AI_INSTRUCTIONS.md` وسجّل التعارض في القسم 10 من `Agent.md`.

في حال تعارض بين متطلب المستخدم في رسالة جديدة وما هو موثق هنا:
- **رسالة المستخدم الحديثة لها الأولوية.**
- وثّق التغيير في Decisions Log قبل تطبيقه.
