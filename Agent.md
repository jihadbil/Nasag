# نَسَق لإدارة المدارس - Agent Plan

> ملف الذاكرة الرسمي وخطة العمل لمشروع نَسَق. أي وكيل AI يدخل المشروع يجب أن يقرأ هذا الملف بالكامل قبل البدء.

---

## 1. Project Overview

**نَسَق لإدارة المدارس** هو برنامج Windows Desktop عربي احترافي لإدارة المدارس، يهدف إلى تقديم نظام عملي وبسيط يغطي الوظائف الفعلية المستخدمة في المدارس الحقيقية:

- إدارة الطلاب وأولياء الأمور
- إدارة الصفوف والشعب
- تسجيل الحضور والغياب اليومي
- إدارة المواد والامتحانات وإدخال الدرجات
- حساب النتائج النهائية
- إدارة الرسوم والأقساط والمدفوعات
- إصدار التقارير
- إدارة المستخدمين والصلاحيات
- إعدادات المدرسة والنسخ الاحتياطي

**النطاق المستبعد صراحةً (لا تطوّر):** LMS، تطبيق ولي أمر، تطبيق طالب، الباصات، المكتبة، الكافتيريا، الدردشة، الدفع الإلكتروني، المحاسبة الكاملة، ميزات الذكاء الاصطناعي داخل البرنامج.

---

## 2. Technology Stack

| المكوّن | القرار |
|---------|--------|
| لغة البرمجة | C# |
| إطار العمل | .NET 8 (net8.0-windows) |
| واجهة المستخدم | WPF |
| النمط المعماري | MVVM |
| قاعدة البيانات | SQL Server (LocalDB في التطوير، Express/Standard في الإنتاج) |
| ORM | Entity Framework Core 8 |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| اللغة | عربية فقط - RTL |
| الخط | Tajawal |
| نظام التشغيل | Windows 10/11 |

> ملاحظة: المشروع الحالي هو WPF .NET 8 فارغ تم إنشاؤه مسبقاً في `Nasag/Nasag.csproj`. سيتم البناء فوقه.

---

## 3. Visual Design Reference

مجلد `UI/` هو **المرجع البصري الأساسي والملزم**. يحتوي على 10 صور تصاميم تغطي الشاشات الرئيسية:

| الملف | الشاشة |
|------|--------|
| (1).png | لوحة التحكم (Dashboard) |
| (2).png | شاشة تسجيل الدخول (Login) |
| (3).png | قائمة الطلاب |
| (4).png | إضافة / تعديل طالب |
| (5).png | الصفوف والشعب |
| (6).png | الحضور والغياب |
| (7).png | إدخال الدرجات |
| (8).png | نتائج الطلاب |
| (9).png | الرسوم والأقساط |
| (10).png | مركز التقارير |

### الهوية البصرية المستخرجة من التصاميم

**التخطيط العام:**
- قائمة جانبية ثابتة على اليمين (Right Sidebar) بخلفية Navy داكنة، تحتوي الشعار في الأعلى ثم عناصر التنقل.
- شريط علوي (Top Bar) أبيض يحتوي: بحث عام، اختيار المدرسة، اختيار السنة الدراسية، إشعارات، اسم المستخدم وصورته.
- منطقة محتوى مركزية بخلفية فاتحة (off-white) تحتوي البطاقات والجداول.

**لوحة الألوان (Color Palette):**

| الاستخدام | اللون (تقريبي) |
|----------|---------------|
| Navy Sidebar (الخلفية الجانبية) | `#0E2A47` |
| Navy Deep (نصوص رئيسية، عناوين) | `#1B3A57` |
| Teal Primary (الأزرار الأساسية، التحديد، التأكيد) | `#1FB5A8` |
| Teal Hover | `#19A294` |
| Background (خلفية المحتوى) | `#F5F7FB` |
| Card Surface | `#FFFFFF` |
| Border / Divider | `#E5E9F0` |
| Text Primary | `#1B3A57` |
| Text Secondary | `#6B7A8F` |
| Success (حاضر، مدفوع، ناجح) | `#22C55E` |
| Warning (متأخر، مستحق) | `#F59E0B` |
| Danger (غائب، متأخر السداد، راسب) | `#EF4444` |
| Info (إجازة، ملاحظة) | `#3B82F6` |

**النمط البصري:**
- بطاقات بيضاء بحواف مدورة (radius ~12px) مع ظل ناعم.
- أزرار أساسية: خلفية Teal + نص أبيض + حواف مدورة.
- أزرار ثانوية: خلفية بيضاء + إطار رمادي فاتح + نص Navy.
- DataGrid نظيف: صفوف بيضاء، فاصل أفقي ناعم، رأس بخلفية فاتحة، أعمدة عمل (تعديل/حذف) كأزرار أيقونية صغيرة.
- شارات الحالة (Status Pills): كبسولات صغيرة بخلفية ملونة شفافة ونص بنفس اللون الكامل.
- الحقول: TextBox / ComboBox / DatePicker بحواف مدورة، إطار رمادي، تركيز Teal.
- المسافات: padding كريم بين العناصر، عدم الازدحام.

**الأيقونات:** نمط outline / line-icons (مثل Lucide أو Phosphor)، بحجم 18-22px في القائمة الجانبية و14-16px داخل الجداول.

**الاتجاه:** `FlowDirection="RightToLeft"` على مستوى التطبيق.

**الخط:** Tajawal بأوزان Regular / Medium / Bold.

---

## 4. Core Rules (قواعد ملزمة)

1. البرنامج عربي بالكامل — جميع النصوص الظاهرة للمستخدم بالعربية.
2. كل الواجهات RTL (FlowDirection = RightToLeft).
3. لا تستخدم وظائف خارج النطاق المذكور في القسم 1.
4. لا تكسر بنية المشروع المعتمدة في القسم 6.
5. لا تكرر الكود — إذا أمكن إنشاء Style / UserControl / Converter مشترك فافعل ذلك.
6. لا تستخدم صور `UI/` كخلفيات أو ImageBrush لتقليد الواجهة — هي مرجع بصري فقط.
7. كل الواجهات يجب أن تكون XAML حقيقية وقابلة للتشغيل والتطوير.
8. كل مرحلة يجب أن تُختبر (Build ناجح + تشغيل + اختبار يدوي للسيناريو الأساسي) قبل الانتقال للتالية.
9. بعد إكمال أي مرحلة، يجب تحديث هذا الملف (قسم 8 Current Progress + قسم 9 Decisions Log).
10. أسماء الجداول والكود إنجليزية، نصوص الواجهة عربية فقط.
11. أصلح الأخطاء من جذورها، لا تخفِ الأعراض.
12. لا تضف ميزات لم تُطلب — التزم بالـ Scope.

---

## 5. Database Plan

قاعدة البيانات: **SQL Server**. اسم القاعدة المقترح: `NasaqSchoolDb`.

### الجداول الأساسية

| الجدول | الوصف | حقول رئيسية |
|--------|-------|-------------|
| `Users` | مستخدمو النظام | Id, Username, PasswordHash, FullName, RoleId, IsActive |
| `Roles` | الأدوار والصلاحيات | Id, NameAr, Permissions (json/bitmask) |
| `SchoolSettings` | بيانات المدرسة | Id, NameAr, LogoPath, Address, Phone, Email, CurrentAcademicYearId |
| `AcademicYears` | السنوات الدراسية | Id, NameAr (مثل "2025 - 2026"), StartDate, EndDate, IsActive |
| `Grades` | الصفوف (الأول، الثاني...) | Id, NameAr, Level (Primary/Middle/High), SortOrder |
| `Sections` | الشعب (أ، ب، ج) | Id, GradeId, NameAr, Capacity, AcademicYearId |
| `Guardians` | أولياء الأمور | Id, FullName, Relation, Phone, Email, NationalId |
| `Students` | الطلاب | Id, StudentNumber, FullName, Gender, BirthDate, NationalId, GradeId, SectionId, GuardianId, PhotoPath, EnrollmentDate, Status (Active/Archived) |
| `Subjects` | المواد | Id, NameAr, GradeId, MaxMark, PassMark |
| `Exams` | أنواع الامتحانات | Id, NameAr (شهري/فصلي/نهائي), AcademicYearId, Weight |
| `Marks` | الدرجات | Id, StudentId, SubjectId, ExamId, Mark, Notes |
| `AttendanceRecords` | الحضور | Id, StudentId, Date, Status (Present/Absent/Late/Excused), Notes |
| `FeePlans` | خطط الرسوم | Id, NameAr, GradeId, TotalAmount, AcademicYearId |
| `StudentFees` | رسوم الطالب | Id, StudentId, FeePlanId, TotalAmount, PaidAmount, RemainingAmount |
| `Installments` | الأقساط | Id, StudentFeeId, InstallmentNumber, DueDate, Amount, Status (Paid/Due/Overdue) |
| `Payments` | سندات القبض | Id, StudentFeeId, InstallmentId, Amount, PaymentDate, ReceiptNumber, Method, UserId, Notes |
| `BackupLogs` | سجل النسخ الاحتياطي | Id, FilePath, CreatedAt, CreatedBy, SizeBytes |

### العلاقات الأساسية
- `Student` → `Section` → `Grade`
- `Student` → `Guardian` (many-to-one، مع إمكانية مشاركة ولي أمر لعدة طلاب)
- `Subject` → `Grade`
- `Mark` → `Student`, `Subject`, `Exam`
- `AttendanceRecord` → `Student`
- `StudentFee` → `Student`, `FeePlan`
- `Installment` → `StudentFee`
- `Payment` → `StudentFee`, `Installment`, `User`

### Seed Data (تجريبية)
- مدرسة: "مدرسة النور الأهلية"
- السنة الدراسية: "2025 - 2026"
- صفوف: الأول الابتدائي … الثالث الثانوي
- شعب: أ، ب، ج
- مستخدم تجريبي: admin / admin123 (مدير النظام)
- ~30 طالب بأسماء عربية واقعية
- مواد، امتحانات، درجات، حضور، رسوم تجريبية

---

## 6. Architecture Plan

البنية المعتمدة (تبنى داخل مشروع `Nasag` الحالي، مع إمكانية تقسيم مستقبلي إلى مشاريع مكتبات):

```
/Nasag
  /Assets
    /Fonts            ← Tajawal-Regular.ttf, Tajawal-Medium.ttf, Tajawal-Bold.ttf
    /Images           ← Logo.png وأيقونات
  /Themes
    Colors.xaml       ← فرشاة الألوان الكاملة
    Typography.xaml   ← أنماط النصوص والعناوين
    Buttons.xaml      ← أنماط الأزرار (Primary, Secondary, Icon, Danger)
    Inputs.xaml       ← أنماط TextBox, PasswordBox, ComboBox, DatePicker
    DataGrid.xaml     ← نمط DataGrid + أعمدة الإجراءات
    Cards.xaml        ← أنماط البطاقات والحاويات
    StatusPills.xaml  ← شارات الحالة
    Icons.xaml        ← Geometry للأيقونات
  /Controls           ← UserControls قابلة لإعادة الاستخدام
    StatCard.xaml
    StatusPill.xaml
    SidebarMenuItem.xaml
    SectionHeader.xaml
  /Views
    /Auth             ← LoginView
    /Shell            ← MainShellView (يحوي Sidebar + TopBar + ContentHost)
    /Dashboard
    /Students
    /Classes
    /Attendance
    /Subjects
    /Marks
    /Results
    /Fees
    /Reports
    /Users
    /Settings
    /Backup
  /ViewModels
    (واحد لكل View، مع BaseViewModel ونظام Navigation)
  /Models             ← Entity classes (Student, Section, ...)
  /Data
    NasaqDbContext.cs
    /Migrations
    DbSeeder.cs
  /Repositories       ← StudentsRepository, AttendanceRepository, ... (واجهات + تنفيذ)
  /Services
    INavigationService / NavigationService
    IAuthService / AuthService
    ICurrentUserService
    IDialogService
    IBackupService
    IReportService
  /Helpers            ← Converters, Extensions, RelayCommand, ObservableObject
  App.xaml
  App.xaml.cs         ← DI Container, Startup
  MainWindow.xaml     ← shell host
```

**أنماط رئيسية:**
- MVVM مع `CommunityToolkit.Mvvm` (RelayCommand + ObservableObject + Source Generators).
- Navigation عبر `INavigationService` بدلاً من Frame البحت.
- Repository Pattern فوق EF Core.
- DI عبر `Microsoft.Extensions.Hosting` في `App.xaml.cs`.

**حزم NuGet المتوقعة (الحد الأدنى):**
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Configuration.Json`
- `CommunityToolkit.Mvvm`
- (اختياري لاحقاً) `QuestPDF` أو `iText` للتقارير، `EPPlus` لـ Excel، `LiveChartsCore.SkiaSharpView.WPF` للرسوم البيانية.

---

## 7. Development Phases

### Phase 0 — Planning and Agent Files
**Status:** ✅ Completed (2026-05-15)

**Tasks:**
- [x] فحص بنية المشروع
- [x] فحص مجلد UI (10 صور)
- [x] استخراج الهوية البصرية والألوان
- [x] تحديد المعمارية
- [x] تحديد الجداول
- [x] إنشاء Agent.md
- [x] إنشاء AI_INSTRUCTIONS.md

**Acceptance Criteria:**
- [x] Agent.md موجود
- [x] AI_INSTRUCTIONS.md موجود
- [x] خطة كاملة موثقة
- [x] لا كود فعلي قبل الموافقة على الانتقال

---

### Phase 1 — Project Foundation
**Status:** ✅ Completed (2026-05-15)

**Tasks:**
- [x] إنشاء بنية المجلدات في `Nasag/` (Assets/Fonts, Assets/Images, Themes, Views, ViewModels, Models, Data, Repositories, Services, Helpers, Controls).
- [x] إضافة حزم NuGet الأساسية: CommunityToolkit.Mvvm 8.3.2، EF Core 8.0.10 (Core + SqlServer + Tools)، Microsoft.Extensions.Hosting 8.0.1 (+ Configuration / Configuration.Json / DependencyInjection).
- [x] تنزيل خط Tajawal (Regular/Medium/Bold) من Google Fonts إلى `Assets/Fonts/` وتسجيله كـ Resource في `.csproj`.
- [x] إنشاء `Themes/Colors.xaml` (لوحة كاملة + Brushes + CornerRadius).
- [x] إنشاء `Themes/Typography.xaml` (TajawalFont + أحجام + أنماط TextBlock).
- [x] إنشاء `Themes/Common.xaml` (defaults ضمنية لـ Window / TextBlock / Control: خط، اتجاه RTL، خلفية، ألوان).
- [x] ضبط `App.xaml` لتحميل الـ ResourceDictionaries الثلاثة عبر MergedDictionaries، وحذف StartupUri لصالح bootstrap يدوي.
- [x] إعداد DI Container في `App.xaml.cs` باستخدام Microsoft.Extensions.Hosting + قراءة `appsettings.json` + تسجيل services + ViewModels.
- [x] إنشاء `IAppInfoService` (سيرفس تجريبي صغير) و`MainViewModel` لإثبات DI.
- [x] تحديث `MainWindow.xaml` كـ smoke-test: بطاقة مركزية بالشعار + اسم البرنامج + شريط Color Swatches للتحقق من اللوحة.
- [x] إنشاء `appsettings.json` بـ ConnectionString لـ LocalDB.
- [x] الاعتماد على CommunityToolkit.Mvvm `ObservableObject` و`RelayCommand` بدلاً من كتابة BaseViewModel يدوي.

**Acceptance Criteria:**
- [x] المشروع يبني بدون أخطاء (Build succeeded: 0 Warning, 0 Error).
- [x] النافذة تفتح بخلفية فاتحة `#F5F7FB` وخط Tajawal مُحمَّل ومرئي.
- [x] DI يعمل: `IAppInfoService` → `MainViewModel` تحقن في `MainWindow.DataContext` بنجاح.
- [x] FlowDirection RTL مفعّل عبر default style على Window في `Common.xaml`.
- [x] Generic Host يبدأ ويسجل `Application started.` بدون استثناءات.

---

### Phase 2 — Design System and Main Shell
**Status:** ✅ Completed (2026-05-15)

**Tasks:**
- [x] بناء `MainShellView` (Window) يحتوي: Sidebar يميناً (عرض 260) + TopBar أعلى + ContentHost في المنتصف.
- [x] Sidebar بخلفية Navy، الشعار في الأعلى، 12 عنصر تنقل: الرئيسية، الطلاب، الصفوف والشعب، الحضور، المواد، إدخال الدرجات، النتائج، الرسوم، التقارير، المستخدمون، الإعدادات، النسخ الاحتياطي.
- [x] TopBar: شريحة مستخدم، شريحة سنة، شريحة مدرسة، صندوق بحث، إشعارات، تسجيل خروج.
- [x] أنماط الأزرار: `PrimaryButton`, `SecondaryButton`, `DangerButton`, `GhostButton`, `IconButton`, `RowActionButton`, `BusyPrimaryButton` في [Themes/Buttons.xaml](Nasag/Themes/Buttons.xaml).
- [x] أنماط الحقول الافتراضية: TextBox, PasswordBox, ComboBox, DatePicker + `FieldLabel` في [Themes/Inputs.xaml](Nasag/Themes/Inputs.xaml).
- [x] نمط DataGrid كامل (Row/Cell/Header) + AlternatingRow في [Themes/DataGrid.xaml](Nasag/Themes/DataGrid.xaml).
- [x] أنماط البطاقات والظلال في [Themes/Cards.xaml](Nasag/Themes/Cards.xaml).
- [x] شارات الحالة (Success/Warning/Danger/Info/Teal) في [Themes/StatusPills.xaml](Nasag/Themes/StatusPills.xaml).
- [x] مكتبة Geometry للأيقونات (22 أيقونة من نمط outline) في [Themes/Icons.xaml](Nasag/Themes/Icons.xaml).
- [x] UserControls: `StatCard`, `SectionHeader`, `SidebarMenuItem`, `LoadingOverlay`, `ConnectionStatusBanner` تحت `Controls/`.
- [x] `IBusyService` / `BusyService` لإدارة حالة Busy + رسالة قابلة للتخصيص.
- [x] `IConnectionMonitor` / `ConnectionMonitor` stub (Phase 3 سيربطه بـ `CanConnectAsync`).
- [x] `INavigationService` / `NavigationService` مع 12 NavigationDescriptor + Resolver عبر IServiceProvider.
- [x] 12 PageViewModel ترث `PageViewModel` (Dashboard/Students/Classes/Attendance/Subjects/Marks/Results/Fees/Reports/Users/Settings/Backup).
- [x] `PagePlaceholderView` موحّد للشاشات قبل بناء كل واحدة لاحقاً.
- [x] `DataTemplates.xaml` يربط كل PageVM بالـ Placeholder (سيُستبدل لاحقاً عند بناء View حقيقي).
- [x] `MainShellViewModel` يدير NavigationItems + IsActive + IsDisconnected + RetryConnectionCommand.
- [x] `ResourceKeyConverter` لربط IconKey (string) بـ Geometry من Application.Current.Resources.
- [x] تسجيل كل services + ViewModels في DI في `App.xaml.cs`.
- [x] استبدال startup إلى MainShellView مع حقن `MainShellViewModel` في `DataContext`.
- [x] إزالة `MainWindow` و`MainViewModel` القديمين (لم يعودا مستخدمين).

**Acceptance Criteria:**
- [x] الشكل العام مطابق للهوية البصرية (Navy sidebar + Teal accents + بطاقات بيضاء + ظل ناعم + RTL).
- [x] التنقل بين 12 قسم يعمل عبر القائمة الجانبية (Highlight + Active strip).
- [x] لا توجد صور UI مستخدمة كخلفية — كل شيء XAML/Geometry.
- [x] الأنماط مركزية في `/Themes` بدون تكرار.
- [x] `LoadingOverlay` جاهز للربط بـ `IsBusy` على أي ViewModel.
- [x] `ConnectionStatusBanner` يظهر/يختفي ديناميكياً من `MainShellViewModel.IsDisconnected`.
- [x] Build: 0 Warning / 0 Error. التطبيق يقلع بدون استثناءات.

---

### Phase 3 — Database and Core Entities
**Status:** ✅ Completed (2026-05-15)

**Tasks:**
- [x] إنشاء كل Model في `/Models` (17 كياناً + Enums): Role, User, SchoolSettings, AcademicYear, Grade, Section, Guardian, Student, Subject, Exam, Mark, AttendanceRecord, FeePlan, StudentFee, Installment, Payment, BackupLog.
- [x] إنشاء [NasaqDbContext](Nasag/Data/NasaqDbContext.cs) مع DbSets والعلاقات الكاملة (Fluent API: أنواع، أطوال، Precision، Indexes فريدة، Delete behaviors).
- [x] ضبط connection string في `appsettings.json` (موجود من Phase 1) + قراءته عبر `IConfiguration` عند تسجيل `AddDbContextFactory<NasaqDbContext>` في DI.
- [x] تفعيل `EnableRetryOnFailure(5, TimeSpan.FromSeconds(10))` في إعداد `UseSqlServer`.
- [x] إنشاء أول Migration `20260515081343_InitialCreate` تحت [Nasag/Data/Migrations/](Nasag/Data/Migrations/).
- [x] **`IDatabaseInitializer` / `DatabaseInitializer` يطبّق Migrations ديناميكياً عند بدء التطبيق:**
  - `GetPendingMigrationsAsync()` ثم `MigrateAsync()` تلقائياً — أي Migration جديدة تُلتقط بلا كود إضافي.
  - `CanConnectAsync()` يُستدعى عندما لا توجد Migrations لضمان الوصول.
  - استدعاء `IDbSeeder.SeedIfEmptyAsync()` بعد Migrations.
  - معالجة أخطاء (SqlException، عام) وإرجاع `DatabaseInitResult` مع `DatabaseInitStatus` (Success/CannotConnect/MigrationFailed/SeedFailed/Unknown) ورسالة عربية.
- [x] لا يوجد أي استخدام لـ `EnsureCreated()` في المشروع.
- [x] `IDesignTimeDbContextFactory<NasaqDbContext>` ([NasaqDbContextFactory.cs](Nasag/Data/NasaqDbContextFactory.cs)) لدعم `dotnet ef` بشكل مستقل عن host بدء التشغيل.
- [x] [DbSeeder](Nasag/Data/DbSeeder.cs) إدراج: 4 أدوار (مدير نظام/مدير مدرسة/معلم/محاسب)، admin/admin123 (BCrypt)، السنة 2025 - 2026، مدرسة النور الأهلية، 12 صف، 12 شعبة (6 صفوف × 2)، 36 مادة (6 صفوف × 6)، 3 امتحانات، 6 خطط رسوم، 30 طالب + 30 ولي أمر + StudentFees + 4 أقساط لكل طالب. Seeder Idempotent عبر فحص `Users.AnyAsync()`.
- [x] [IRepository<T> + Repository<T>](Nasag/Repositories/) عام async (GetAll, Where, GetById, FirstOrDefault, Count, Add, Update, Delete + OpenScope() لاستعلامات IQueryable). مسجّل كـ open generic singleton في DI.
- [x] [App.OnStartup](Nasag/App.xaml.cs) أصبح async: يُهيّئ Host → يستدعي `IDatabaseInitializer.InitializeAsync()` عبر `Task.Run` لتجنب deadlock — عند الفشل يظهر MessageBox عربي ويُغلق التطبيق (Phase 13 ستستبدل MessageBox بـ Splash + Setup Wizard).
- [x] [IConnectionMonitor](Nasag/Services/IConnectionMonitor.cs) مربوط الآن فعلياً بـ `IDbContextFactory<NasaqDbContext>.CreateDbContextAsync().Database.CanConnectAsync()` بدلاً من stub Phase 2، مع dispatch إلى UI thread عند تعديل الحالة.
- [x] [StudentsViewModel](Nasag/ViewModels/Pages/PageViewModel.cs) يستهلك `IRepository<Student>` ويُحمّل عدد الطلاب async في `ActivateAsync` المُستدعى من `MainShellViewModel.RefreshFromNavigation`.

**Acceptance Criteria:**
- [x] قاعدة البيانات تُنشأ وتُحدَّث تلقائياً عند أول تشغيل عبر Migrations (`DatabaseInitializer.MigrateAsync`).
- [x] إضافة Migration جديد لاحقاً يُطبَّق ذاتياً (المنطق يعتمد على `GetPendingMigrationsAsync`).
- [x] Seed Data تُحقن مرة واحدة فقط — Seeder يتحقق من `Users.AnyAsync()` قبل الإدراج.
- [x] قراءة Students من Repository داخل `StudentsViewModel.ActivateAsync` async.
- [x] فشل الاتصال يُعرض كرسالة عربية واضحة عبر MessageBox + Shutdown، لا crash.
- [x] Build: 0 Warning / 0 Error.

---

### Phase 4 — Authentication and Users
**Status:** ✅ Completed (2026-05-15)

**Tasks:**
- [x] [LoginView](Nasag/Views/Auth/LoginView.xaml) — تخطيط من جزأين: لوحة Navy يسار (شعار + اسم البرنامج + شريحة وصف) + لوحة بيضاء يمين (نموذج الدخول): عنوان + رسالة خطأ + اسم المستخدم + كلمة المرور + تذكّرني + رابط نسيت كلمة المرور + زر Teal أساسي + قسيمة الحساب التجريبي (admin/admin123).
- [x] [IAuthService](Nasag/Services/IAuthService.cs) + [AuthService](Nasag/Services/AuthService.cs): يبحث المستخدم async، يفحص `IsActive`، يتحقق من BCrypt.Verify، يحدّث `LastLoginAt`، يعيد `AuthResult` بحالات (Success/InvalidCredentials/AccountDisabled/ConnectionError/Unknown) ورسائل عربية.
- [x] [ICurrentUserService](Nasag/Services/ICurrentUserService.cs) + [CurrentUserService](Nasag/Services/CurrentUserService.cs): يحفظ `User` الحالي + يولّد `DisplayName`/`Initial` + يبث `SignedIn`/`SignedOut`.
- [x] [LoginViewModel](Nasag/ViewModels/Auth/LoginViewModel.cs): Username/Password/RememberMe/ErrorMessage/IsBusy + `LoginCommand` async مع `CanExecute` يمنع الإرسال أثناء التحميل أو عند فراغ الحقول.
- [x] PasswordBox مربوط عبر code-behind بـ `vm.Password` (WPF لا يسمح bind مباشر لأسباب أمنية).
- [x] [App.OnStartup](Nasag/App.xaml.cs) أصبح مدير دورة حياة: يُهيّئ Host → DB → يفتح `LoginView`؛ عند `SignedIn` يفتح `MainShellView` ويُغلق Login؛ عند `SignedOut` يفعل العكس؛ `ShutdownMode=OnExplicitShutdown` في `App.xaml` لمنع الإغلاق التلقائي أثناء التبديل.
- [x] [MainShellViewModel](Nasag/ViewModels/Shell/MainShellViewModel.cs): يحقن `ICurrentUserService`، يعرض `UserDisplayName`/`UserInitial`/`UserRoleName` (مع تحديث ديناميكي عبر `SignedIn/Out`)، أضاف `LogoutCommand`، يُعيد التنقل إلى Dashboard تلقائياً عند تسجيل دخول جديد لتجنّب deep-link مزدوج (لأن الـ VM Singleton).
- [x] [MainShellView TopBar](Nasag/Views/Shell/MainShellView.xaml): شريحة المستخدم تعرض الحرف الأول + الاسم + اسم الدور؛ زر تسجيل الخروج مربوط بـ `LogoutCommand`.
- [x] [Converters.xaml](Nasag/Themes/Converters.xaml) + [InverseBoolToVisibilityConverter](Nasag/Helpers/InverseBoolToVisibilityConverter.cs) جديدان (سيُستخدمان في الشاشات اللاحقة أيضاً).

**Acceptance Criteria:**
- [x] الدخول بـ `admin` / `admin123` يعمل ويفتح MainShellView (BCrypt verify ناجح على hash السي).
- [x] كلمة مرور خاطئة تُظهر بانر أحمر بالعربية: «اسم المستخدم أو كلمة المرور غير صحيحة.».
- [x] حساب موقوف يُظهر رسالة مخصصة: «هذا الحساب موقوف. يرجى التواصل مع المدير.».
- [x] فشل الاتصال بـ SQL أثناء الدخول يُعرض كرسالة عربية واضحة لا crash.
- [x] اسم المستخدم وحرفه الأول واسم دوره ظاهرون في TopBar؛ زر تسجيل الخروج يُعيد المستخدم إلى نافذة Login.
- [x] Build: 0 Warning / 0 Error؛ التطبيق يقلع ويعرض شاشة Login بدون استثناءات.

---

### Phase 5 — Dashboard
**Status:** ✅ Completed (2026-05-15)

**Tasks:**
- [x] إضافة حزمة `LiveChartsCore.SkiaSharpView.WPF` 2.0.0-rc5.4 + إخفاء تحذيرات NU1701 الناتجة عن SkiaSharp/OpenTK عبر `<NoWarn>$(NoWarn);NU1701</NoWarn>`.
- [x] [IDashboardService](Nasag/Services/IDashboardService.cs) + [DashboardService](Nasag/Services/DashboardService.cs): استدعاء واحد `GetSnapshotAsync` يُرجع `DashboardSnapshot` مكوّناً من `DashboardStats` + `AttendanceLast7Days` + `AttendanceBreakdown` (اليوم) + `DashboardAlerts` + `RecentActivity[]`. كل الاستعلامات async عبر `IDbContextFactory<NasaqDbContext>` (CountAsync/SumAsync/GroupBy + projection بدون tracking).
- [x] [DashboardViewModel](Nasag/ViewModels/Pages/DashboardViewModel.cs) منفصل عن `PageViewModel.cs`: حقن `IDashboardService`، خصائص ObservableProperty لكل البطاقات والكسور، `ObservableCollection<RecentActivity> RecentActivities`، تحضير `Axis[]` و`ISeries[]` لكل من خط الحضور (LineSeries<double>) ودونات اليوم (PieSeries<double> 4 شرائح Success/Danger/Warning/Info)، Empty-state آمن: شريحة رمادية واحدة عندما لا توجد سجلات حضور لليوم. `RefreshCommand` للتحديث اليدوي. `ActivateAsync` يُستدعى تلقائياً من Shell عند الدخول للقسم.
- [x] [DashboardView.xaml](Nasag/Views/Pages/DashboardView.xaml) مطابق للتصميم (1): شريط رأس مع زر تحديث Secondary، صف 5 بطاقات إحصائية (StatCard موحّد بألوان مختلفة: Teal/Info/Navy/Danger/Success)، صف الرسوم بنسبة 2:1 (Line Chart 240px + Donut 200px مع نسبة الحضور المركزية ومفتاح ألوان UniformGrid 2×2)، صف سفلي 1:2 (4 بطاقات تنبيهات يسار + قائمة آخر الأنشطة يمين بفواصل سفلية)، `LoadingOverlay` فوق كامل الشاشة.
- [x] الرسوم تُغلَّف داخل `FlowDirection="LeftToRight"` لتجنّب عكس محاور SkiaSharp، مع overlay عربي RTL يُعرض بانر "لا توجد سجلات حضور بعد" عند `HasAttendanceHistory=false`.
- [x] [BoolToVisibilityConverter](Nasag/Helpers/BoolToVisibilityConverter.cs) + [ActivityKindConverters](Nasag/Helpers/ActivityKindConverters.cs) (Brush + Icon) + [DateToRelativeArabicConverter](Nasag/Helpers/DateToRelativeArabicConverter.cs) (يطبع: الآن / قبل دقيقة(دقيقتين/N) / قبل ساعة(ساعتين/N) / أمس / قبل يومين / قبل N أيام / yyyy/MM/dd) — كلها في `/Helpers`.
- [x] [DataTemplates.xaml](Nasag/Themes/DataTemplates.xaml): استبدال `PagePlaceholderView` بـ `DashboardView` لـ `DashboardViewModel`.
- [x] [App.xaml.cs](Nasag/App.xaml.cs): تسجيل `IDashboardService → DashboardService` Singleton.
- [x] حذف الـ stub `DashboardViewModel` القديم من `PageViewModel.cs` لصالح الملف المنفصل.
- [x] إصلاح ترويسة الصفحة: استبدال DockPanel بـ Grid عمودَين بعد ملاحظة أن `DockPanel.Dock="Right"` و`HorizontalAlignment="Left"` معاً في وضع RTL يدفعان العنوان والزر إلى نفس الجانب البصري (اليسار) بدل توزيعهما. الحل النهائي: Column 0 (`*`) للعنوان `HorizontalAlignment="Right"` يُرسم على اليمين، Column 1 (`Auto`) للزر يُرسم على اليسار.

**Acceptance Criteria:**
- [x] الأرقام تُحسب من قاعدة البيانات الحقيقية: عدد الطلاب النشطين، الشعب، المواد، إجمالي المحصّل، إجمالي المتبقي، أقساط متأخرة، أقساط مستحقة هذا الأسبوع — كلها من Seeder الفعلي (30 طالب، 12 شعبة، 36 مادة، StudentFees + 4 أقساط لكل طالب).
- [x] الرسم البياني يعرض بيانات حقيقية من `AttendanceRecords` بتجميع per-day GroupBy + Empty-state واضح ("لا توجد سجلات حضور بعد") عندما يكون الجدول فارغاً (الحالة الراهنة لأن Seeder لم يُدخل حضوراً).
- [x] الدونات يعرض شريحة رمادية واحدة عندما لا توجد بيانات حضور لليوم بدلاً من فشل LiveCharts بمصفوفة فارغة.
- [x] RTL سليم: الواجهة كاملة `FlowDirection=RightToLeft`، الرسوم وحدها LeftToRight لتجنّب عكس المحاور.
- [x] `LoadingOverlay` يظهر أثناء `LoadAsync` ويختفي في `finally` حتى لو فشل الاستعلام.
- [x] فشل DB يُعرض في `StatusMessage` بدلاً من crash.
- [x] Build: 0 Warning / 0 Error. التطبيق يقلع، يتصل بـ DB، Login يفتح بدون استثناء (الدخول الفعلي للوحة يتطلب تفاعل مستخدم).

---

### Phase 6 — Students and Guardians
**Status:** ✅ Completed (2026-05-15)

**Tasks:**
- [x] [IStudentsRepository](Nasag/Repositories/IStudentsRepository.cs) + [StudentsRepository](Nasag/Repositories/StudentsRepository.cs) متخصص: `SearchAsync` (بحث + فلترة حسب صف/شعبة/حالة + Pagination + Include للعلاقات + Projection لـ `StudentRow`)، `GetStatsAsync` (Total/Active/Archived عبر GroupBy)، `GetLookupsAsync` (الصفوف والشعب)، `GetForEditAsync` (Projection شامل لـ `StudentEditorPayload` بكل بيانات الطالب وولي الأمر)، `CreateAsync` و`UpdateAsync` كلاهما داخل Transaction (إنشاء/تحديث Guardian + Student معاً)، `SetStatusAsync` للأرشفة/الاستعادة، `StudentNumberExistsAsync` لفحص التفرد، `NextStudentNumberAsync` لتوليد رقم تسلسلي تلقائي.
- [x] [IDialogService](Nasag/Services/IDialogService.cs) + [DialogService](Nasag/Services/DialogService.cs): تأكيد/معلومة/خطأ بـ `MessageBoxOptions.RtlReading | RightAlign` على Dispatcher مع TaskCompletionSource ليعمل من background.
- [x] [IFileService](Nasag/Services/IFileService.cs) + [FileService](Nasag/Services/FileService.cs): `PickImage` (Microsoft.Win32.OpenFileDialog مع فلتر صور) + `SaveStudentPhotoAsync` (نسخ آمن إلى `%LocalAppData%/Nasaq/Photos/Students/{guid}.ext` async، يبقي الأصل سليماً).
- [x] [StudentsViewModel](Nasag/ViewModels/Pages/Students/StudentsViewModel.cs): إدارة الوضع (List/Editor)، فلاتر (SearchText بـ Debounce 300ms، GradeOption، SectionOption مفلترة حسب Grade المختار، StudentStatusFilter)، Pagination (Page/PageSize/PageSizeOptions/TotalCount/TotalPages مع NotifyCanExecuteChanged للأزرار)، Stats (Total/Active/Archived)، Commands (ReloadCommand/AddStudent/EditStudent/ArchiveStudent/RestoreStudent/ClearFilters/NextPage/PrevPage)، Messaging مع Editor عبر Saved/Cancelled events.
- [x] [StudentEditorViewModel](Nasag/ViewModels/Pages/Students/StudentEditorViewModel.cs): دورتا حياة (LoadForCreateAsync مع NextStudentNumberAsync، LoadForEditAsync مع GetForEditAsync)، حقول الطالب وولي الأمر، اختيار صف يفلتر الشعب الظاهرة، StagedPhotoSource (مسار محلي قبل الحفظ) + PhotoPath (المسار النهائي بعد النسخ)، PickPhoto/RemovePhoto، Validation عربي، SaveAsync (يفحص StudentNumberExistsAsync ثم ينسخ الصورة عبر SaveStudentPhotoAsync ثم Create/Update)، Cancel، EnumOption<T> generic record للقوائم المنسدلة العربية للجنس وصلة القرابة.
- [x] [StudentsView.xaml](Nasag/Views/Pages/Students/StudentsView.xaml) مطابق للتصميم 3: Header (عنوان يميناً + زر تحديث يساراً) + 3 stat cards (Total/Active/Archived) + Toolbar (بحث + 4 ComboBoxes فلاتر + زر "مسح الفلاتر" + زر "إضافة طالب" Primary) + DataGrid (رقم الطالب، الاسم بـ avatar دائري بلون Teal مع الحرف الأول، الصف، الشعبة، Status pill ملوّنة، الجوال، ولي الأمر، عمود إجراءات: تعديل/أرشفة/استعادة) + Empty-state ودود + Pagination footer (السابق/التالي + label "الصفحة X من Y — إجمالي N").
- [x] [StudentEditorView.xaml](Nasag/Views/Pages/Students/StudentEditorView.xaml) مطابق للتصميم 4: Header (Breadcrumb "الطلاب › إضافة/تعديل" + زر "رجوع للقائمة") + Error banner عربي بـ DataTrigger + 3 بطاقات مرقّمة (1: بيانات الطالب — اسم/رقم/جنس/تاريخ ميلاد/هوية/جوال/صف/شعبة/تاريخ تسجيل، 2: بيانات ولي الأمر — اسم/صلة/جوال/جوال احتياطي/إيميل/هوية/مهنة، 3: العنوان والملاحظات) + لوحة الصورة الجانبية (200×200 placeholder + ImageBrush للصورة + اختر/إزالة + قائمة الصيغ المدعومة) + Footer (إلغاء + حفظ Primary) + ربط `IsEnabled` بـ `IsBusy` عبر InverseBoolConverter جديد.
- [x] Helpers جديدة: [StudentConverters](Nasag/Helpers/StudentConverters.cs) (StudentStatus → عربي/Background/Foreground/Equals، Gender → عربي، InitialLetter للحرف الأول، PathToImageSource بـ BitmapImage مجمَّد، StringNotEmptyToBool) + [InverseBoolConverter](Nasag/Helpers/InverseBoolConverter.cs).
- [x] [DataTemplates.xaml](Nasag/Themes/DataTemplates.xaml): استبدال PagePlaceholderView لـ StudentsViewModel بـ StudentsView (إضافة namespaces vmStudents/viewsStudents).
- [x] [App.xaml.cs](Nasag/App.xaml.cs) DI: تسجيل IStudentsRepository، IDialogService، IFileService، StudentEditorViewModel، StudentsViewModel من namespace جديد.
- [x] [NavigationService.cs](Nasag/Services/NavigationService.cs): تحديث using لـ `Nasag.ViewModels.Pages.Students`.
- [x] حذف الـ stub القديم من `PageViewModel.cs` للنسخة المتقدمة.

**Acceptance Criteria:**
- [x] إضافة طالب جديد وحفظه (مع Guardian جديد ضمن Transaction واحدة).
- [x] تعديل طالب موجود (يحمّل بيانات الطالب وولي الأمر، يحفظ كلاهما معاً).
- [x] أرشفة طالب (تأكيد عبر DialogService، Status=Archived، يختفي من فلتر "نشط")، واستعادته يعيده Active.
- [x] البحث يعمل لحظياً مع Debounce 300ms (اسم/رقم طالب/هوية/جوال/اسم ولي الأمر).
- [x] الفلاتر (الصف، الشعبة، الحالة، حجم الصفحة) تعيد التحميل تلقائياً مع إعادة Page=1.
- [x] Pagination صحيح: السابق/التالي يُعطّلان عند الحدود، Label يحدّث.
- [x] Photo upload يعمل: نسخ إلى LocalAppData دون تعديل الأصل، عرض ImageBrush للمعاينة.
- [x] Build: 0 Warning / 0 Error.

---

### Phase 7 — Grades and Sections
**Status:** ✅ Completed (2026-05-16)

**Tasks:**
- [x] [IClassesRepository](Nasag/Repositories/IClassesRepository.cs) + [ClassesRepository](Nasag/Repositories/ClassesRepository.cs): استعلامات `GetGradesAsync` (صفوف + عدد الشعب + عدد الطلاب النشطين في السنة الحالية)، `GetSectionsForGradeAsync` (شعب الصف مع `StudentCount`/`Capacity`/`Remaining`/`IsOverCapacity`)، `GetStudentsForSectionAsync` (قائمة الطلاب للشعبة المختارة)، `GetStatsAsync` (إجماليات الصفوف/الشعب/الطلاب)، `GetCurrentAcademicYearIdAsync` (من SchoolSettings ثم Active fallback). كل العمليات الكتابية داخل Transaction عبر `CreateExecutionStrategy()`.
- [x] CRUD الصفوف: `CreateGradeAsync` / `UpdateGradeAsync` / `DeleteGradeAsync` (cascade). الحذف يمسح بترتيب آمن: لكل شعبة → Payments → Installments → StudentFees → Attendance → Marks → Students → Guardians اليتيمى → Section → Subjects + Marks الخاصة بالصف → FeePlans + StudentFees + Installments + Payments المرتبطة → Grade.
- [x] CRUD الشعب: `CreateSectionAsync` (التحقق من سنة دراسية نشطة + فحص تعارض الاسم لنفس الصف والسنة)، `UpdateSectionAsync` (فحص تعارض)، `DeleteSectionAsync` (cascade الطلاب وكل تبعياتهم).
- [x] `MoveStudentAsync(studentId, targetSectionId)` يتحقق من السعة المتبقية ويرفض النقل عند الامتلاء برسالة عربية واضحة.
- [x] `GetMoveTargetsAsync(excludeStudentId)` يُرجع شعب السنة الحالية مع العداد والسعة جاهزة للعرض في `SearchableComboBox`.
- [x] [ClassesViewModel](Nasag/ViewModels/Pages/Classes/ClassesViewModel.cs) منفصل (يحل محل stub في PageViewModel.cs): حقن `IClassesRepository`/`IDialogService`/`IToastService`/`IErrorReporter`، Init-guard pattern + Reload re-entrance guard، خصائص `Grades`/`Sections`/`StudentsInSection`/`Stats` كـ `ObservableCollection`/`ObservableProperty`، `OnSelectedGradeChanged` يعيد تحميل الشعب، `OnSelectedSectionChanged` يعيد تحميل الطلاب، `SubtitleAr` يُحدَّث ديناميكياً مع الإحصائيات، Commands: `AddGrade/EditGrade/DeleteGrade/AddSection(CanExecute=HasSelectedGrade)/EditSection/DeleteSection/MoveStudent/Reload`.
- [x] تأكيد حذف مزدوج: ConfirmDestructiveAsync أول للنية، ثم ConfirmDestructiveAsync ثانٍ يعرض عدد الشعب/الطلاب/المواد التي سيتم حذفها (عبر `GetGradeDependencyCountsAsync` / `GetSectionDependencyCountsAsync`).
- [x] [ClassesView.xaml](Nasag/Views/Pages/Classes/ClassesView.xaml) مطابق للتصميم 5: Header يميناً (FlowDirection Swap Pattern موثَّق في UI Standards) + زر تحديث يساراً، Body Grid عمودين: قائمة صفوف يمين (عرض 300 + Footer بزر BubbleButton لإضافة صف + ListBox مخصص بحدّ علوي تيل عند الاختيار + إجراءات تعديل/حذف لكل صف)، شطر يسار به بطاقتان رأسياً (شعب الصف 280px أسفل من *) و(طلاب الشعبة المختارة).
- [x] الـ DataGrid للشعب: عمود الشعبة، عدد الطلاب، السعة، المتبقي (يتحول للأحمر عند IsOverCapacity)، إجراءات تعديل/حذف. الـ DataGrid للطلاب: رقم/اسم بـ avatar/حالة Pill ملونة/جوال/زر «نقل» بأيقونة سهم.
- [x] 3 Dialogs مخصصة بنمط NasaqDialog: [GradeEditorDialog](Nasag/Views/Pages/Classes/Dialogs/GradeEditorDialog.xaml) (الاسم + المرحلة كـ ComboBox + ترتيب العرض)، [SectionEditorDialog](Nasag/Views/Pages/Classes/Dialogs/SectionEditorDialog.xaml) (الاسم + السعة)، [MoveStudentDialog](Nasag/Views/Pages/Classes/Dialogs/MoveStudentDialog.xaml) (SearchableComboBox من `MoveTargetSection` يعرض "الصف — الشعبة (count/capacity)" مع فحص الامتلاء قبل التأكيد).
- [x] Move action متاح من شاشتين: من `ClassesView` على كل صف في جدول طلاب الشعبة، ومن `StudentsView` كزر صف ثالث في عمود الإجراءات (`StudentsViewModel` يحقن `IClassesRepository` مباشرة لتجنّب الاعتماد على `ClassesViewModel`).
- [x] [DataTemplates.xaml](Nasag/Themes/DataTemplates.xaml): استبدال PagePlaceholderView لـ ClassesViewModel بـ ClassesView (إضافة namespaces `vmClasses`/`viewsClasses`).
- [x] [App.xaml.cs](Nasag/App.xaml.cs): تسجيل `IClassesRepository → ClassesRepository` Singleton + ضبط using لـ `Nasag.ViewModels.Pages.Classes`.
- [x] [NavigationService.cs](Nasag/Services/NavigationService.cs): تحديث using إلى namespace الجديد لـ `ClassesViewModel`.
- [x] حذف stub `ClassesViewModel` من `ViewModels/Pages/PageViewModel.cs`.
- [x] اختصارات لوحة المفاتيح: `F5` تحديث، `Ctrl+N` إضافة شعبة (للصف المختار).

**Acceptance Criteria:**
- [x] إنشاء صف جديد وإضافة شعب له يعمل ضمن السنة الدراسية الحالية.
- [x] تعديل اسم الصف، المرحلة، أو ترتيبه ينعكس فوراً في القائمة بعد Reload.
- [x] التحقق من السعة عند نقل الطالب: الشعبة الممتلئة ترفض النقل مع رسالة عربية، وعداد الطلاب/السعة يظهر في كل قائمة هدف.
- [x] حذف صف بـ cascade: تأكيد مزدوج + إزالة كل الشعب والطلاب والتبعيات في Transaction واحدة. مماثل لحذف شعبة بداخلها طلاب.
- [x] نقل طالب بين الشعب يعمل من ClassesView ومن StudentsView، وSelectedSection في ClassesView يتحدّث بعد كل عملية.
- [x] فشل DB يُعرض عبر `IErrorReporter` (ErrorWindow)، الأخطاء المنطقية (validation/سعة ممتلئة) عبر `IDialogService.ShowWarningAsync`.
- [x] Build: 0 Warning / 0 Error.

---

### Phase 8 — Attendance
**Status:** ✅ Completed (2026-05-16)

**Tasks:**
- [x] شاشة `الحضور والغياب` (تصميم 6): اختيار صف/شعبة/تاريخ، بطاقات إحصائية (إجمالي/حاضر/غائب/متأخر/إجازة)، DataGrid بأعمدة (رقم الطالب، اسم الطالب، حاضر/غائب/متأخر/إجازة كأزرار راديو لكل صف، ملاحظات).
- [x] زر "تحديد الكل حاضر".
- [x] زر حفظ يحفظ سجلات اليوم.
- [x] منع تكرار سجل لنفس الطالب في نفس اليوم (Upsert) عبر `AttendanceRepository.SaveAttendanceSheetAsync`.
- [x] `IAttendanceRepository` + `AttendanceRepository`: Lookups للسنة الدراسية الحالية، تحميل الطلاب النشطين فقط، تحميل السجل المحفوظ لنفس التاريخ، وحفظ Transactional مع `CreateExecutionStrategy()`.
- [x] `AttendanceViewModel` منفصل في `ViewModels/Pages/Attendance`: Init/reload guards، أوامر Reload/Save/MarkAllPresent، عدادات ديناميكية، Notes بحد 300 حرف، وحالة Dirty.
- [x] `DataTemplates.xaml` يعرض `AttendanceView` الحقيقي بدلاً من placeholder + تسجيل `IAttendanceRepository` في DI.

**Acceptance Criteria:**
- [x] تسجيل حضور شعبة كاملة وحفظها.
- [x] إعادة فتح نفس اليوم تُظهر السجلات المحفوظة.
- [x] الملخصات تتحدث ديناميكياً.
- [x] Build: 0 Warning / 0 Error.

---

### Phase 9 — Subjects, Exams, Marks, Results
**Status:** ✅ Completed (2026-05-18)

**Tasks:**
- [x] شاشة `المواد الدراسية` (CRUD): قائمة + فلتر بالصف + بحث + Dialog إضافة/تعديل (NameAr، Grade، MaxMark، PassMark). الحذف يرفض عند وجود درجات مسجلة.
- [x] شاشة `أنواع الامتحانات` (CRUD): قائمة + فلتر بالسنة الدراسية + Dialog إضافة/تعديل (NameAr، Year، Weight 0.1-10). يقبل الفاصلة العربية `٫` و `،`. الحذف يرفض عند وجود درجات.
- [x] شاشة `إدخال الدرجات` (تصميم 7): اختيار صف/شعبة/مادة/امتحان → DataGrid طلاب يحرَّر فيه عمود الدرجة + ملاحظات + Save Transactional (Upsert على الفهرس الفريد StudentId+SubjectId+ExamId)؛ Value=null يحذف الدرجة الموجودة (تمييز «غير ممتحن»). validation: 0 ≤ Value ≤ Subject.MaxMark بـ InvalidOperationException عربي.
- [x] شاشة `نتائج الطلاب` (تصميم 8): فلاتر صف/شعبة/سنة + 5 بطاقات إحصائية (إجمالي/ناجحون/راسبون/أعلى/أدنى) + DataGrid (StudentNumber، الاسم، Total/MaxTotal، Percentage، Grade pill، Status pill). البحث محلي بـ Debounce 300ms.
- [x] `IResultsCalculator` (Pure logic، DI Singleton): يحسب المعدل المرجَّح لكل مادة من الامتحانات المُعطاة `weightedSum/weightSum` (ضمناً يستثني الامتحانات الغائبة لتجنّب عقاب الطالب)، النجاح يتطلب نجاحاً في كل المواد بدون أي مادة غير ممتحنة، التقدير: 90+ ممتاز / 80+ جيد جداً / 70+ جيد / 50+ مقبول / دون ذلك راسب.
- [x] إضافة `NavigationSection.Exams` + عنصر القائمة الجانبية «أنواع الامتحانات» بأيقونة IconCalendar + repointing «إدخال الدرجات» إلى IconResults.
- [x] DI: `ISubjectsRepository`، `IExamsRepository`، `IMarksRepository`، `IResultsRepository`، `IResultsCalculator` كـ Singletons + `ExamsViewModel` Singleton (الباقي مسجَّل سابقاً كـ stubs).
- [x] DataTemplates.xaml: ربط Subjects/Exams/Marks/Results بنماذجها الحقيقية (استبدال PagePlaceholderView).
- [x] حذف stubs المراحل من `ViewModels/Pages/PageViewModel.cs` (SubjectsViewModel/MarksViewModel/ResultsViewModel) — الـ ExamsViewModel كان غير موجود أصلاً وأُنشئ كنوع جديد في namespace `Nasag.ViewModels.Pages.Exams`.

**Acceptance Criteria:**
- [x] CRUD المواد يعمل (إضافة/تعديل/حذف) مع منع حذف مادة لها درجات.
- [x] CRUD الامتحانات يعمل (إضافة/تعديل/حذف) مع منع حذف امتحان له درجات.
- [x] إدخال درجات شعبة كاملة لمادة وامتحان بسرعة (Save Transactional عبر ExecutionStrategy).
- [x] إعادة فتح نفس (Section, Subject, Exam) تعرض الدرجات المحفوظة سابقاً.
- [x] حذف درجة بمسح الـ Value يعمل (لا INSERT صفر).
- [x] النتائج تُحسب صحيحة وفق Weight: متوسط مرجَّح لكل مادة + Total عبر المواد + Percentage + Grade pill ملوّن.
- [x] شاشة النتائج تعرض بيانات حقيقية لكل شعبة في سنة محددة.
- [x] Build: 0 Warning / 0 Error.

---

### Phase 10 — Fees and Installments
**Status:** ✅ Completed (2026-05-19)

**Tasks:**
- شاشة `الرسوم والأقساط` (تصميم 9): اختيار طالب، بطاقة بيانات الطالب، بطاقات (إجمالي/مدفوع/متبقي)، جدول الأقساط (رقم، تاريخ الاستحقاق، المبلغ، الحالة، تاريخ الدفع، إجراءات)، زر تسجيل دفعة.
- نموذج تسجيل دفعة (مبلغ، طريقة، ملاحظات) → ينشئ Payment ويحدّث Installment وStudentFee.
- سند قبض مطبوع/Preview.
- كشف حساب طالب.

**Acceptance Criteria:**
- تسجيل دفعة تُحدّث المتبقي.
- قسط مدفوع يتغير لونه/حالته.
- توليد سند قبض برقم تسلسلي.

---

### Phase 11 — Reports and Printing
**Status:** ✅ Completed (2026-05-21)

**Tasks:**
- شاشة `التقارير` (تصميم 10): فلاتر علوية، 4 بطاقات تقارير (الرسوم، الدرجات، الحضور، الطلاب)، جدول آخر التقارير.
- تقارير أساسية: قائمة الطلاب، كشف حضور لفترة، كشف درجات، كشف رسوم.
- تصدير PDF (QuestPDF) وExcel (EPPlus).
- معاينة قبل الطباعة.

**Acceptance Criteria:**
- توليد PDF عربي RTL صحيح.
- تصدير Excel بأعمدة منسقة.
- معاينة تعمل.

---

### Phase 12 — Settings and Backup
**Status:** ✅ Completed (2026-05-21)

**Tasks:**
- [x] شاشة إعدادات المدرسة: اسم، شعار (LogoBytes varbinary)، عنوان، هاتف، إيميل، الموقع، اسم المدير، السنة الدراسية الحالية + قائمة inline للسنوات بإضافة/تعديل/حذف.
- [x] شاشة المستخدمين والأدوار: قائمة + إضافة/تعديل + إعادة تعيين كلمة مرور + تفعيل/تعطيل + حذف + إدارة صلاحيات الأدوار (12 صلاحية بأسماء عربية، قفل آخر admin role).
- [x] النسخ الاحتياطي: زر إنشاء (BACKUP DATABASE WITH FORMAT, INIT, COMPRESSION + fallback)، زر استرجاع (SET SINGLE_USER → RESTORE WITH REPLACE → SET MULTI_USER + Shutdown)، جدول سجل النسخ بأكشن فتح المجلد وحذف السجل.
- [x] EF migration `AddSettingsLogoAndBackupKind` (LogoBytes + BackupLog.Kind).

**Acceptance Criteria:**
- [x] تعديل بيانات المدرسة يُحفظ ويظهر في الشريط العلوي (ReceiptDocument/StatementDocument/4 ReportDocuments تقرأ من SchoolSettings).
- [x] نسخة احتياطية تُنشأ بنجاح + سجل audit + ملف .bak على القرص في المجلد المختار.
- [x] استرجاع نسخة يعمل (بعد تأكيد destructive) ويُغلق التطبيق لإعادة تشغيل نظيف.
- [x] حماية آخر admin من التعطيل/الحذف/تجريد ManageUsers.
- [x] فلترة Sidebar بحسب `Permission.ManageSettings`/`ManageUsers`/`ManageBackup`.
- [x] Build 0/0.

---

### Phase 13 — Final Polish, Splash, Setup Wizard, and Testing
**Status:** ✅ Completed (2026-05-21)

**Tasks:**

#### Splash Screen + Database Pipeline
- إنشاء `SplashWindow` احترافي يظهر فور تشغيل التطبيق بدلاً من MainWindow مباشرة.
- يحوي: الشعار، اسم البرنامج، شريط تقدّم أو سبيرنر، نص حالة ديناميكي بالعربية.
- داخل Splash يجري ما يلي بالترتيب (كل خطوة تُعرض كنص حالة):
  1. "جاري التحقق من الاتصال بقاعدة البيانات…"
  2. "جاري التحقق من التحديثات…" (`GetPendingMigrationsAsync`)
  3. إذا كانت هناك Migrations معلّقة: "جاري تحديث قاعدة البيانات…"
  4. "جاري تحميل البيانات الأولية…" (إن لزم)
  5. "جاهز" → فتح Login ثم MainShell.
- في حالة فشل أي خطوة: عرض رسالة عربية + زر "إعادة المحاولة" + زر "فتح معالج الإعداد".

#### First-Run Setup Wizard
- إذا تعذّر الاتصال بقاعدة البيانات أو كانت `appsettings.json` لا تحوي اتصالاً صالحاً، يفتح `SetupWizardWindow` بدلاً من Splash.
- خطوات المعالج:
  1. **مرحبا بك:** ترحيب وشرح أن البرنامج يحتاج لقاعدة بيانات SQL Server.
  2. **اختر نوع الإعداد:** (LocalDB / SQL Server شبكي).
  3. **بيانات الاتصال:** الخادم، طريقة المصادقة (Windows / SQL Authentication)، اسم المستخدم وكلمة المرور، اسم القاعدة.
  4. **اختبار الاتصال:** زر يجرّب الاتصال ويظهر النتيجة فوراً.
  5. **إنشاء قاعدة جديدة أو الاتصال بموجودة:** إن لم تكن القاعدة موجودة، خيار إنشائها وتطبيق Migrations + Seed.
  6. **إنهاء:** حفظ Connection String مُشفَّر في `appsettings.json` أو ملف إعداد مستخدم محمي، ثم متابعة إلى Splash العادي.
- يُتاح فتح المعالج لاحقاً من شاشة الإعدادات (Phase 12) لإعادة الضبط.

#### Polish
- مراجعة شاملة لـ RTL في كل شاشة.
- توحيد المسافات والألوان.
- مراجعة معالجة الأخطاء (try/catch + DialogService) في كل عمليات DB.
- التأكد أن كل عملية تستغرق وقتاً تعرض `LoadingOverlay` أو حالة Busy.
- التأكد أن `ConnectionStatusBanner` يعمل عند فصل الكابل/إيقاف SQL Server يدوياً.
- تحسين الأداء (Async، Pagination، Lazy load).
- تنظيف الكود وإزالة TODOs.
- اختبار سيناريوهات end-to-end:
  - تنصيب جديد على جهاز نظيف → معالج الإعداد → دخول → عمل كامل.
  - ترقية قاعدة بإضافة Migration جديد → Splash يطبّقه تلقائياً.
  - قطع الاتصال أثناء العمل → ظهور البانر → عودة الاتصال → اختفاء البانر.
- تحديث Agent.md نهائياً.
- كتابة ملف README.md للمستخدم النهائي.

**Acceptance Criteria:**
- Build نظيف بدون warnings حرجة.
- كل شاشات Phase 1-12 تعمل دون أخطاء.
- Splash يظهر دائماً، يحدّث القاعدة تلقائياً، ويفتح المعالج عند الفشل.
- المعالج يستطيع إنشاء قاعدة جديدة على LocalDB أو الاتصال بـ SQL Server بعيد.
- بانر انقطاع الاتصال يعمل في الحالتين (انقطاع وعودة).
- Agent.md محدّث بكل الإنجازات.

---

### Phase 14 — Licensing & Activation + Velopack Updates + Decompilation Protection
**Status:** ✅ Completed (2026-05-22)

**هدف المرحلة:** إضافة نظام تفعيل احترافي بأدوات مجانية بالكامل + تحديث تلقائي عبر Velopack + حماية من فك التجميع، وكل ذلك دون أي اعتماد على خدمات سحابية مدفوعة أو شهادات Authenticode.

**الحل سيتوسّع من مشروع واحد إلى 4 مشاريع:**
1. `Nasag/Nasag.csproj` (موجود) — التطبيق الرئيسي + دمج الترخيص + Velopack.
2. `Nasag.Licensing/Nasag.Licensing.csproj` (Class Library) — مشترك بين التطبيق الرئيسي وأداة المورّد.
3. `NasaqVendor/NasaqVendor.csproj` (WPF App) — أداة المورّد المنفصلة لإصدار التراخيص.
4. `NasaqPackager/NasaqPackager.csproj` (WPF App) — أداة التحزيم والتوزيع.

**Tasks:**

#### Nasag.Licensing (Class Library)
- `MachineFingerprint`: 5 hashes (CPU `Win32_Processor.ProcessorId` + Motherboard `Win32_BaseBoard.SerialNumber` + BIOS `Win32_BIOS.SerialNumber` + `HKLM\...\Cryptography\MachineGuid` + أول MAC فيزيائي غير افتراضي) عبر `System.Management 8.0.0`. SHA-256 لكل مكوّن + composite hash، N-of-M matching (3 من 5) لتحمّل تبديل قطعة أو NIC swap.
- `EcdsaSigner`: P-256 (`ECCurve.NamedCurves.nistP256`). `Sign(bytes, privateKey)` و `Verify(bytes, signature, publicKey)` عبر `ExportECPrivateKey` / `ImportSubjectPublicKeyInfo`.
- `LicenseFile`: JSON schema بـ `v, customerId, customerName, machineHashes[], issuedAtUtc, expiresAtUtc?, edition, features[], signature`. Canonical serialization (sorted keys، UTF-8، no whitespace) لتوقيع مستقر.
- `LicenseValidator`: يفحص (أ) signature صحيح ضد PublicKey المضمَّن، (ب) `machineHashes` يطابق الجهاز الحالي (3 من 5)، (ج) `expiresAtUtc` بعد UtcNow، (د) لا tamper. يرجّع `LicenseStatus { Trial(daysLeft), Activated, Expired, TamperedClock, MachineMismatch, InvalidSignature, Missing }`.
- `TrialManager`: 30 يوماً من أول تشغيل. يخزّن `TrialStartUtc + HMAC(fingerprint, TrialStartUtc)` في `%LOCALAPPDATA%\Nasaq\state.dat` (DPAPI `CurrentUser`) + Registry mirror `HKCU\Software\Nasaq\State`. على غياب الملف (مع وجود Velopack install marker) → trial expired فوراً.
- `ClockTamperDetector`: high-watermark `LastSeenUtc` يُكتب كل 30 دقيقة + on shutdown. مقارنة DPAPI + Registry — `now < max - 1h` ⇒ Tampered. sanity check ضد `File.GetLastWriteTimeUtc(@"C:\Windows\System32\kernel32.dll")`.
- `ProtectedStateStore`: DPAPI wrapper + Registry mirror مع HMAC bound to fingerprint.
- `EmbeddedPublicKey`: قراءة المفتاح العام من EmbeddedResource في التطبيق المُستضيف.
- `AntiTamper`: `Debugger.IsAttached` + timing check + assembly hash (SHA-256 على `Assembly.Location` يقارن بـ baseline من packaging).
- لا UI داخل هذه المكتبة، لا dependencies على WPF.

#### NasaqVendor (WPF App)
- Borderless RTL window (نمط `NasaqDialog`/`SetupWizardWindow`): WindowStyle=None + AllowsTransparency + Card + drag header.
- SQLite عبر `Microsoft.Data.Sqlite 8.0.10` + `Dapper 2.1.35`. ملف `%LOCALAPPDATA%\NasaqVendor\vendor.db`.
- جداول: `Customers (Id, Code, Name, Phone, Email, City, Notes, CreatedAtUtc)` + `Licenses (Id, CustomerId FK, Edition, Features json, MachineHashesJson, IssuedAtUtc, ExpiresAtUtc?, LicenseFilePath, Revoked)` + `IssueAudit (Id, LicenseId, Action, AtUtc, Operator, Notes)`.
- المفتاح الخاص ECDSA يُحفظ في `%LOCALAPPDATA%\NasaqVendor\issuer.key` مشفّر بـ DPAPI. عند أول تشغيل: dialog يطلب (أ) توليد زوج مفاتيح جديد + تصدير المفتاح العام إلى `issuer.public.key` (يضعه المطوّر يدوياً في `Nasag/Resources/`)، أو (ب) استيراد مفتاح خاص قائم.
- 3 شاشات في tab/sidebar صغير: `العملاء` (CRUD + بحث) + `التراخيص` (قائمة + إصدار جديد + سجل التدقيق) + `إعدادات المفتاح`.
- إصدار ترخيص: Dialog يختار العميل + يلصق machine hashes (5 سطور من العميل) + edition + features (checklist) + ExpiresAtUtc اختياري + يولّد `.naslic` ويحفظه + يضيف صفوف Licenses + IssueAudit ضمن transaction.
- تصدير ملف `.naslic` عبر SaveFileDialog.
- إعادة إصدار/إبطال ترخيص (Revoked=1) بسجل audit.

#### NasaqPackager (WPF App)
- Borderless RTL window (نفس نمط NasaqDialog).
- 3 أزرار رئيسية كبيرة (BubbleButton tealxxl):
  1. **«زيادة رقم النسخة»**: يقرأ `<Version>` من `Nasag.csproj` عبر `XDocument`، يزيد الـ patch بـ 1، يحفظ. يعرض النسخة القديمة → الجديدة في الـ log.
  2. **«تحزيم وإصدار»**: ينفّذ `dotnet publish -c Release -r win-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false -o .\publish\nasaq` → `obfuscar.console .\Obfuscar.xml` → `vpk pack --packId Nasaq --packVersion {ver} --packDir .\publish\nasaq-obf --mainExe Nasag.exe --icon .\assets\nasaq.ico --outputDir .\Releases\Customer --channel win`. ProcessStartInfo بـ stdout redirected إلى log panel real-time.
  3. **«فتح مجلد الإصدارات»**: `Process.Start("explorer.exe", releasesPath)`.
- يعرض في الرأس: النسخة الحالية + مسار مشروع Nasag + مسار Releases.
- Log panel (TextBox/RichTextBox أسود نصّ تيل) قابل للنسخ + زر Clear.
- إعدادات: مسار مشروع Nasag، مسار Releases، Channel name، Icon path — يُحفظ في `%LOCALAPPDATA%\NasaqPackager\settings.json`.
- زرّ ثانوي «تشغيل obfuscar فقط» و «تشغيل publish فقط» للتشخيص.

#### Nasag (التطبيق الرئيسي) — التكامل
- إضافة Reference إلى `Nasag.Licensing.csproj` في `Nasag.csproj`.
- إضافة `Velopack 0.0.1298` + `<Version>1.14.0</Version>` + `<StartupObject>Nasag.App</StartupObject>` + EmbeddedResource للمفتاح العام `Resources\issuer.public.key`.
- `App.xaml.cs`: إنشاء `[STAThread] public static void Main(string[] args)` يستدعي `VelopackApp.Build().Run()` **قبل** أي WPF init، ثم `var app = new App(); app.InitializeComponent(); app.Run();`.
- `ILicenseService` + `LicenseService` (Singleton DI): facade فوق Nasag.Licensing، يحمّل `%LOCALAPPDATA%\Nasaq\license.naslic`، يحسب `LicenseStatus`، يفعّل ترخيصاً، يبدأ trial. خصائص observable: `Status`, `DaysRemaining`, `IsActivated`.
- `LicenseGateWindow`: يفتح بعد Splash + بعد إعداد DB لكن **قبل** Login إذا كان `Status != Activated && Status != Trial`. شاشة Navy احترافية بعنوان كبير + شارة حالة (Expired/Tampered/Invalid/Missing) + زرّيْن: «تفعيل البرنامج» (يفتح ActivationWindow) و «إغلاق».
- `ActivationWindow`: borderless wizard 4 خطوات بنمط `SetupWizardWindow`:
  1. **مرحباً**: شرح خطوات التفعيل + اختيار «أملك ملف ترخيص» / «اطلب ترخيص جديد».
  2. **رمز الجهاز**: يعرض 5 hashes للجهاز الحالي + زر «نسخ كل البيانات» (نص متعدد الأسطر يحوي الـ hashes + اسم الجهاز + ربما اسم المدرسة). يطلب من المستخدم إرساله للمورّد.
  3. **تحميل الترخيص**: drop zone قابل للنقر لاختيار `.naslic` + textarea لصق محتوى الملف نصّياً (للذين يستلمونه عبر Email). يعرض معاينة (اسم العميل + الإصدار + الميزات + تاريخ الانتهاء) بعد التحقق الناجح.
  4. **تم!**: confetti بسيط + رسالة شكر + زر «دخول البرنامج» يحفظ `.naslic` في `%LOCALAPPDATA%\Nasaq\license.naslic` (DPAPI optional) + يعيد تشغيل التطبيق لتطبيق الترخيص.
- شارة TopBar: أيقونة قفل/شهادة + نص «النسخة التجريبية — متبقي N يوم» (أصفر < 7 يوم) أو «مفعّل — مدرسة الأمل» (أخضر) أو «منتهي» (أحمر).
- بطاقة Settings جديدة «إدارة الترخيص» بين بطاقة الـ DB والـ Preferences: IconCertificate bubble + الحالة + الإصدار + تاريخ الانتهاء + زر «تفعيل/إعادة تفعيل» (يفتح ActivationWindow) + «نسخ رمز الجهاز» + «إلغاء التفعيل» (يحذف license.naslic بعد تأكيد).
- بطاقة Settings ثانية جديدة «التحديثات» مع IconDownload + النسخة الحالية + زر «التحقق من التحديثات» (يفتح `UpdateWindow`).
- `UpdateWindow`: نافذة مستقلة 480×320 borderless: يعرض «جاري التحقق…» → «أنت تستخدم أحدث نسخة» أو «تتوفر نسخة جديدة X.Y.Z — هل تريد التحديث؟» مع زر «حمّل وحدّث» يظهر progress bar (0-100%) عبر `DownloadUpdatesAsync(progress => ...)` → ينتهي بـ «جاهز للتطبيق» وزر «أعد التشغيل والتحديث» يستدعي `ApplyUpdatesAndRestart`. يدعم الإلغاء والـ resume.
- التحقق التلقائي عند بدء التشغيل: `IUpdateService.CheckForUpdatesSilentlyAsync()` يُستدعى fire-and-forget بعد دخول MainShell بـ 5 ثوانٍ، عند وجود تحديث يُظهر Toast Info «تتوفر نسخة جديدة — انقر هنا للتفاصيل» يفتح `UpdateWindow` عند النقر.
- مصدر التحديثات: `SimpleFileSource` يشير إلى مجلد محلي قابل للتكوين في `appsettings.json` أو `prefs.json` (يدعم مسار شبكة، USB، أو URL لاحقاً).
- أيقونات جديدة في `Themes/Icons.xaml`: `IconShield`, `IconCertificate`, `IconDownload`, `IconPackage`, `IconKey`.

#### Obfuscation + Build Pipeline
- `Obfuscar.xml` في جذر الحل: `HideStrings=true`, `RenameProperties=true`, `RegenerateDebugInfo=false`, `UseUnicodeNames=true`, SkipType لكل `Nasag.ViewModels.*` و `Nasag.Data.*` و `Nasag.Views.*` و `Nasag.Converters.*` و `Nasag.Helpers.*` (XAML bindings + EF entities + DI types).
- `dotnet tool install -g Obfuscar.GlobalTool` (موثّق في README).
- `dotnet tool install -g vpk` (موثّق في README).
- `<DebugType>none</DebugType>` + `<DebugSymbols>false</DebugSymbols>` في Release configuration لجميع المشاريع الـ 4.
- `build-installer.ps1` في جذر الحل: ينفّذ publish + obfuscar + vpk pack ثم يخرج `Releases\Customer\Setup.exe` (للعملاء) و `Releases\Vendor\Setup.exe` (للمورّد المنفصل) — نفس سكربت Packager لكن مستقل عن الـ UI.
- README.md: قسم جديد للمطوّر «بناء النسخة وإصدار التراخيص» بخطوات تثبيت `vpk` + `obfuscar` + كيفية توليد المفتاح + بناء أول إصدار.

**Acceptance Criteria:**
- البرنامج يبدأ بنسخة تجريبية 30 يوماً تلقائياً عند أول تشغيل (لا تفعيل مطلوب).
- محاولة تشغيل البرنامج بعد انتهاء التجربة (أو تغيير ساعة النظام للأمام ثم الخلف) تعرض `LicenseGateWindow` تلقائياً.
- ActivationWindow يعرض رمز الجهاز بوضوح + يقبل ملف `.naslic` صحيح + يرفض الملفات الموقّعة بمفتاح خاطئ أو لجهاز آخر برسالة عربية واضحة.
- بعد التفعيل: شارة TopBar تتحوّل إلى «مفعّل»، Settings تعرض تفاصيل الترخيص، البرنامج لا يعرض LicenseGate في تشغيلاته اللاحقة.
- نقل ملف `license.naslic` فقط إلى جهاز آخر لا يفعّل البرنامج (machine hashes mismatch).
- تغيير ساعة النظام للماضي بعد التشغيل يُكتشف على التشغيل التالي.
- `NasaqVendor.exe` يفتح، يولّد مفتاحاً، يضيف عميلاً، ويصدر `.naslic` ينجح تفعيله في `Nasag.exe`.
- `NasaqPackager.exe` يزيد النسخة في csproj، ينفّذ publish + obfuscar + vpk pack بدون أخطاء، ينتج Setup.exe قابل للتشغيل.
- التحديث: تشغيل Setup.exe لإصدار أحدث على نسخة مثبّتة سابقاً يفتح UpdateWindow ويحدّث بنجاح، البيانات والترخيص يبقيان.
- فحص `dotPeek`/`dnSpyEx` على `Nasag.exe` المُحزَّم يُظهر أسماء classes/methods مشوَّشة (Unicode + abc) — رمز الترخيص غير قابل للقراءة بسهولة.
- Build 0/0 للمشاريع الـ 4.

---

## 8. Current Progress

| Phase | Status | Started | Completed | Notes |
|-------|--------|---------|-----------|-------|
| Phase 0 — Planning | ✅ Completed | 2026-05-15 | 2026-05-15 | تم فحص المشروع و10 صور UI، استخراج الهوية البصرية، إنشاء Agent.md وAI_INSTRUCTIONS.md |
| Phase 1 — Foundation | ✅ Completed | 2026-05-15 | 2026-05-15 | بنية مجلدات + 8 حزم NuGet + خط Tajawal (3 أوزان) + Colors/Typography/Common dictionaries + DI عبر Generic Host + smoke-test window. Build: 0/0. |
| Phase 2 — Shell & Design System | ✅ Completed | 2026-05-15 | 2026-05-15 | MainShell كاملاً (Sidebar+TopBar+ContentHost) + 7 ResourceDictionaries (Buttons/Inputs/DataGrid/Cards/Pills/Icons/DataTemplates) + 5 UserControls (StatCard/SectionHeader/SidebarMenuItem/LoadingOverlay/ConnectionBanner) + IBusyService/IConnectionMonitor/INavigationService + 12 Page VMs + Placeholder view. Build 0/0. |
| Phase 3 — Database | ✅ Completed | 2026-05-15 | 2026-05-15 | 17 Entities + Enums + NasaqDbContext (Fluent API كامل) + InitialCreate migration + IDatabaseInitializer (Migrate ديناميكياً) + DbSeeder (4 أدوار/admin/12 صف/12 شعبة/36 مادة/30 طالب/خطط رسوم) + IRepository<T>/Repository<T> + ConnectionMonitor متصل بـ CanConnectAsync + EnableRetryOnFailure + BCrypt للتجزئة. Build 0/0. |
| Phase 4 — Auth | ✅ Completed | 2026-05-15 | 2026-05-15 | LoginView بتخطيط جزأين (Navy brand + form بيضاء) + IAuthService (BCrypt verify) + ICurrentUserService بـ SignedIn/Out events + LoginViewModel + App.OnStartup يدير دورة Login↔Shell + ShutdownMode=OnExplicitShutdown + TopBar يعرض الاسم/الحرف/الدور + LogoutCommand. Converters.xaml + InverseBoolToVisibility. Build 0/0. |
| Phase 5 — Dashboard | ✅ Completed | 2026-05-15 | 2026-05-15 | LiveCharts2 (rc5.4) + IDashboardService/DashboardService snapshot واحد + DashboardViewModel منفصل (5 stat cards + line chart للحضور 7 أيام + donut اليوم + 4 بطاقات تنبيهات + قائمة آخر الأنشطة) + DashboardView مطابق للتصميم (1) + Empty-states آمنة لكل جدول فارغ (LoadingOverlay، Refresh، StatusMessage عند الفشل) + 3 Converters جديدة (BoolToVisibility/ActivityKind*/DateToRelativeArabic) + NoWarn NU1701. Build 0/0. |
| Phase 6 — Students | ✅ Completed | 2026-05-15 | 2026-05-15 | StudentsView (تصميم 3) + StudentEditorView (تصميم 4) + StudentsViewModel/StudentEditorViewModel + IStudentsRepository (Search/Filter/Paginate/Stats/Lookups/CreateUpdate transactional/Archive) + IDialogService/DialogService (RTL MessageBox) + IFileService/FileService (Photo picker + copy إلى LocalAppData) + 6 converters جديدة + InverseBoolConverter. Debounce بحث 300ms، Pagination مع NotifyCanExecuteChanged، Status pills ملوّنة، Avatar بالحرف الأول. Build 0/0. |
| Phase 6.1 — Cross-cutting hardening | ✅ Completed | 2026-05-15 | 2026-05-15 | إصلاح خطأ EnableRetryOnFailure مع BeginTransaction (CreateExecutionStrategy.ExecuteAsync) + نقل الصور إلى DB (Student.PhotoBytes varbinary(max) + migration AddStudentPhotoBytes + StudentSaveModel.UpdatePhoto + BytesToImageSourceConverter) + IErrorReporter/ErrorReporter/ErrorWindow (Dispatcher/AppDomain/TaskScheduler hooks، نسخ كامل التفاصيل) + IToastService/ToastService/ToastHost (Success/Error/Warning/Info auto-dismiss 4s) + إعادة تخطيط شاشات الطلاب (no page scroll، DataGrid يحتوي السكرول، pagination ثابت أسفل، action bar للـ Editor ثابت أعلى، إزالة بطاقات stats الكبيرة لصالح سطر مدمج في الترويسة) + AI_INSTRUCTIONS.md محدّث بقواعد UX جديدة. Build 0/0. |
| Phase 6.2 — UI Standards + Import/Export | ✅ Completed | 2026-05-15 | 2026-05-15 | Remember Me فعّال عبر `IUserPreferencesService` (JSON في LocalAppData) + Toast Notifications منقولة للزاوية اليسرى (FlowDirection LTR منفصل) + `NasaqDialog` بديل احترافي لـ `MessageBox` (Confirm/Destructive/Info/Success/Warning/Error) + `SearchableComboBox` بحث/اقتراحات/مسح + DataGrid theme جديد (Center + Full GridLines) + `BubbleButton` Style كزر CTA + Students Page Redesign كامل (header يمين، إزالة stats، toolbar single-row بـ Bubble Add + 4 SearchableComboBox + Search + Refresh/Clear مختلفان + Export/Import) + خيار «ترتيب أبجدي» في SettingsView يُحفظ في prefs (newest-first fallback) + Pagination ComboBox قابل للكتابة للقفز السريع + Delete row action + Double-click open + Keyboard shortcuts (Ctrl+N, F5, Delete, Ctrl+F, Ctrl+S, Esc) + StudentEditor: Photo dropzone قابل للنقر كاملاً + ClosedXML Excel Service (Export احترافي 20 عمود عربي) + Import Wizard متعدد الخطوات (PickFile → Preview → Mode Append/Replace → Result) + StudentsRepository.DeleteAsync/DeleteAllStudentsAsync/BulkInsertAsync/GetAllForExportAsync/StudentSortMode + توثيق UI Standards كاملاً في AI_INSTRUCTIONS.md. Build 0/0. |
| Phase 6.3 — Combobox + Photo + Delete polish | ✅ Completed | 2026-05-16 | 2026-05-16 | قالب `ComboBox` كامل جديد في Inputs.xaml (RTL + Placeholder + Chevron + فتح بنقرة واحدة) — Toggle يغطي الحقل كاملاً والنص فوقه `IsHitTestVisible=False`. `SearchableComboBox`: RTL داخل حقل البحث، Commit موثوق عند النقر في أي مكان من العنصر (VisualTreeHelper walk-up)، مزامنة النص بعد إغلاق الـ Popup. StudentEditor: الصورة Overlay خارج قالب الزر — تحديث المصدر فوراً بعد الاختيار. التحقق من إمكانية عرض الصورة قبل قبولها + Toast واضح. حذف الطالب عبر `ExecuteDelete` متدرّج (Payments → Installments → Fees → Attendance → Marks → Student → Guardian اليتيم). إعادة ضبط `IsHitTestVisible` في ToastHost/MainShellView/StudentsView لاستعادة تفاعل الـ FAB. Build 0/0. |
| Phase 7 — Classes | ✅ Completed | 2026-05-16 | 2026-05-16 | IClassesRepository + ClassesRepository (Grades/Sections/Students lookups + Stats + Move + Cascade Delete) + ClassesViewModel منفصل + ClassesView مطابق للتصميم 5 (قائمة صفوف يمين + Cards يسار: شعب الصف + طلاب الشعبة المختارة) + 3 Dialogs (GradeEditorDialog، SectionEditorDialog، MoveStudentDialog مع SearchableComboBox) + تأكيد حذف مزدوج للـ Cascade + Move action في الشاشتين (ClassesView جدول الطلاب + StudentsView عمود الإجراءات) + Capacity validation + AcademicYear-aware queries. DataTemplates + DI + NavigationService محدَّثون. Build 0/0. |
| Phase 8 — Attendance | ✅ Completed | 2026-05-16 | 2026-05-16 | AttendanceRepository + AttendanceViewModel منفصل + AttendanceView مطابق للتصميم 6: اختيار صف/شعبة/تاريخ، بطاقات إجمالي/حاضر/غائب/متأخر/إجازة، DataGrid تحرير مباشر، تحديد الكل حاضر، حفظ Upsert لسجلات اليوم مع Date.Date وطلاب نشطين فقط. Build 0/0. |
| Phase 9 — Subjects/Exams/Marks/Results | ✅ Completed | 2026-05-18 | 2026-05-18 | 4 ميزات + Calculator مشترك: (1) Subjects CRUD مع منع حذف عند وجود درجات. (2) Exams CRUD بـ Weight + اختيار السنة الدراسية. (3) Marks Entry sheet (نمط Attendance): اختيار Grade→Section→Subject→Exam → DataGrid قابل للتحرير + Save Transactional عبر ExecutionStrategy (Upsert + حذف عند Value=null) + validation 0..MaxMark. (4) Results view: فلاتر + 5 بطاقات إحصائية + DataGrid بـ Grade pill ملوّن. IResultsCalculator (Pure logic) يحسب المعدل المرجَّح لكل مادة (يستثني الامتحانات الغائبة من weightSum)، Grade: ممتاز/جيد جداً/جيد/مقبول/راسب على النسبة المئوية، النجاح يتطلب لا فشل ولا غياب لأي مادة. إضافة NavigationSection.Exams + عنصر قائمة جانبية + DI لـ 4 Repositories + Calculator. تنفّذ عبر 4 وكلاء بالتوازي + دمج نهائي يدوي. Build 0/0. |
| Phase 9.1 — Phase 9 Polish | ✅ Completed | 2026-05-18 | 2026-05-18 | دورة مراجعة-إصلاح-تدقيق بـ 10 وكلاء متخصصين بناءً على screenshots من المستخدم. أبرز التحسينات: (1) **Calculator**: إضافة `ResultGrade.Pending` + حقل `ExaminedMax` — الآن النسبة تُحسب على المواد المُمتحَنة فقط (75/100 = 75% بدل 75/600 = 12.5%)، والطلاب الذين امتُحنوا جزئياً يظهرون «غير مكتمل» (Warning soft) بدلاً من «راسب» مزيَّف. حدود التقدير ثوابت `static readonly`. (2) **Results VM/View**: `PendingCount` + بطاقة إحصائية سادسة، Highest/Lowest تستثني Pending، `StatusLabelAr` ثلاثي (ناجح/راسب/غير مكتمل)، Pills تدعم Pending، TotalDisplay يستخدم ExaminedMax + tooltip للمجموع الكلي. (3) **Marks View**: أعمدة Toolbar مرنة (Auto/MinWidth + `*`) — لا قطع لـ «الدراسات الاجتماعية»، ToolTip على الـ ComboBoxes، عمود الملاحظات `2*` بدلاً من `2*` للاسم، DataGrid `KeyboardNavigation.TabNavigation=Continue`، DataTrigger يلوّن خانة الدرجة Danger soft عند `Value < PassMark`، `Visibility` على DataGrid عند `HasRows=False`. (4) **Marks VM**: catch منفصل لـ `InvalidOperationException` كـ Toast Warning، تحذير عند التنقّل بتعديلات غير محفوظة. (5) **Marks Repo**: validation عبر HashSet/distinctIds (بدل المقارنة الخاطئة بالأعداد). (6) **Dialogs**: `FlowDirection=RightToLeft` في ExamEditorDialog، استبدال `ComboBox` بـ `SearchableComboBox` في كلا الـ Editor Dialogs، ترجمة الرسالة الإنجليزية الوحيدة في ExamsRepository. (7) **ExamsView**: زر مسح الفلاتر أصبح أيقونياً (IconFilterClear). (8) **SubjectsViewModel**: نمط `try/finally` للـ init-guard. Build 0/0. تدقيق نهائي مستقل: ✅ جاهز للقبول. |
| Phase 10 — Fees and Installments | ✅ Completed | 2026-05-19 | 2026-05-21 | مرحلة كاملة لميزة الرسوم والأقساط مُنفَّذة كوحدة واحدة عبر دورات متعددة الوكلاء (مراجعة + تخطيط + تنفيذ متوازٍ + تدقيق نهائي مستقل + إصلاحات Blockers). **الشاشة (تصميم 9):** Single-student workflow — اختيار صف→شعبة→طالب + حقل بحث سريع برقم الطالب (Enter يقفز للطالب)، بطاقة ملخّص الطالب مع `TealPill` لاسم الخطة، 3 بطاقات إحصائية بترتيب صحيح (الرسوم→المدفوع→المتبقي من اليمين) مع `SuccessPill` للنسبة المدفوعة، جدولان متجاوران (تفاصيل الأقساط + سجل الدفعات) بأعمدة محسَّنة + Empty states بأيقونة 64px + CTA gated بالصلاحية، شريط أكشن سفلي ثابت (إجمالي المتبقي + كشف حساب + طباعة آخر سند + تسجيل دفعة). **بطاقة CTA «تعيين خطة رسوم»** تظهر تلقائياً عند غياب الخطة للطالب في السنة الحالية. **الدايلوجات** (PaymentDialog + AssignFeePlanDialog): Borderless RTL بنمط موحَّد، عنوان وصفي + زر × close + drag مقيَّد بالرأس، Banner أحمر صحيح (Border DangerSoft + DangerBrush) للأخطاء، AmountBox يفرمت بفاصل آلاف + يحدّث الاقتراح عند تغيير القسط، يقبل الفاصلة العربية `٫`/`،`. **`IFeesRepository` / `FeesRepository`** بمنطق كامل: `GetLookups` (سنة دراسية حالية)، `GetStudentsForSection`، `GetStudentDetails` (DTO حتى بدون خطة، `AsSplitQuery` + Projection)، `RecordPayment` (Transactional عبر ExecutionStrategy، توليد رقم سند `REC-yyyyMMdd-nnnn` ذرّي مع retry ×3 على UNIQUE violation 2601/2627، `ExecuteUpdateAsync` على `PaidAmount` لمنع dirty write، رفض الدفع الزائد على مستويين، رفض تواريخ مستقبلية)، `DeletePayment` (يعكس المبالغ ذرّياً + يُعيد حساب Status)، `AssignFeePlan` (تحقق تطابق الصف والسنة، رفض التكرار، InstallmentsCount>0 + TotalAmount>0 + Student.Active، توزيع الأقساط من `AcademicYear.StartDate` بفترات متساوية مع دحرجة الباقي)، `RecomputeOverdueAsync` (`ExecuteUpdate` ذرّي يضبط Overdue للأقساط منتهية الصلاحية مرة لكل جلسة)، `LocateStudentByNumber`، `GetAssignablePlans`، `GetSchoolHeader` (لحظي لا cache). **الصلاحيات:** فرض `Permission.ManageFees` داخل الـ Repository عبر `EnsurePermission` (لا VM-only)، `ICurrentUserService.HasPermission(Permission)` helper، إخفاء أزرار الكتابة بـ `Visibility=Collapsed` للمستخدم بلا صلاحية، فلترة Sidebar في `MainShellViewModel.RebuildNavigationItems()` بحسب `RequiredPermission?` على `NavigationDescriptor` + إعادة بناء عند SignedIn/SignedOut + إعادة توجيه لـ Dashboard عند فقدان الصلاحية للقسم الحالي. **الطباعة:** طبقة `Services/Printing/` نقية بـ WPF FlowDocument + PrintDialog + DocumentViewer (لا NuGet جديد). `ReceiptDocument` (سند قبض A4 RTL Tajawal + ترويسة مدرسية + المبلغ رقماً وكتابةً + توقيعات)، `StatementDocument` (كشف حساب: معلومات الطالب + ملخص الخطة + جدول الأقساط + جدول الدفعات)، `ArabicNumberWords` (0..999,999,999.99 + حالات خاصة: 0/Major=0/Minor=0)، `PrintPreviewWindow` بنمط NasaqDialog. **العملة:** `MoneyFormatter` مركزي بثقافة `ar-SA` بدل " ر.س" hard-coded في 8 مواقع. **التكامل عبر البرنامج:** زر «فتح الرسوم» في عمود إجراءات `StudentsView` (gated بـ `CanManageFees`) → `NavigateTo(Fees)` ثم `PrepareForStudentAsync(int)` يحمّل lookups → يحدد الصف والشعبة → يستعيد الطالب (Singleton VM)، اشتراك على `ICurrentUserService.SignedIn/SignedOut` لتحديث الحالة فورياً، `IConnectionMonitor` guard قبل الكتابة، `OperationCanceledException` يُعاد رميه قبل كل catch عام. **Seeder:** بذرة 10-12 طالب × 1-2 دفعة عشوائية (30-50%) مع تحديث `Installment.PaidAmount/StudentFee.PaidAmount/Status` ثم ضبط `Overdue` للأقساط منتهية الصلاحية ضمن ExecutionStrategy+Transaction مع الحفاظ على idempotency. **Converters جديدة:** `InstallmentStatusToArabic/Background/Foreground` + `PaymentMethodToArabic` + `DecimalToCurrencyArabic` + `InstallmentNotPaidToVisibility` + `FirstItemConverter`. **KeyBindings:** Ctrl+N/F5/Ctrl+P/Ctrl+F. **UI Standards:** `RowActionButton` أيقوني في أعمدة الإجراءات، `OverlayBrush` و `BackgroundBrush` بدلاً من ألوان hard-coded، `SearchableComboBox` لكل القوائم. Build: 0/0. |
| Phase 11 — Reports | ✅ Completed | 2026-05-21 | 2026-05-21 | مرحلة كاملة لمركز التقارير والطباعة، نُفّذت كوحدة واحدة عبر دورة متعددة الوكلاء (3 وكلاء Explore متوازين للاستطلاع + 5 وكلاء تنفيذ متوازين مع فصل صارم للملفات + وكيل تدقيق نهائي مستقل + إصلاحات Major/Minor). **العقد المركزي:** `IReportsRepository.cs` يجمع كل DTOs الـ 4 تقارير (Students/Attendance/Marks/Fees) + `ReportLookups` (Grades/Sections/Exams بالسنة الحالية) + `SchoolHeaderInfo` مُعاد استخدامها من IFeesRepository. **شاشة Reports Hub:** 4 بطاقات بأيقونات ملوّنة (Students teal/Attendance success/Marks info/Fees amber) + UniformGrid responsive + DataGrid «آخر التقارير» (Session memory، capped at 50) مع إجراءات Re-open/Delete/Clear + Empty state بأيقونة 64px. **4 دايلوجات بنمط PaymentDialog القياسي:** borderless RTL، عنوان + ×، أزرار سفلية (معاينة وطباعة Primary + تصدير PDF/Excel Secondary + إغلاق Ghost)، KeyBindings (Ctrl+P معاينة، Esc إغلاق)، فلترة بـ `SearchableComboBox`. **`ReportsRepository`:** تجميعات بـ Projections + AsNoTracking + Correlated subqueries لتجنّب N+1، يحترم `SchoolSettings.CurrentAcademicYearId`، يرجّع نتائج فارغة بأمان عند غياب السنة. الحضور **يحتفظ بالحقيقة التاريخية** (لا يفلتر بـ Status==Active لأن الطالب قد يكون مؤرشَفاً الآن لكنه كان نشطاً في الفترة). نسبة الحضور = (Present + Late × 0.5) / RecordedDays × 100. الدرجات تستخدم `IResultsCalculator.Compute` في مسار aggregate (بدون امتحان محدد) للحفاظ على `ExaminedMax` وحالة «غير مكتمل» الموحَّدة، مع تصحيح دلالة `IsAbsent` (فقط عندما `Score != null` ومُسجَّل غياباً حقيقياً، لا «درجة غير مدخلة»). فلتر الرسوم `Unpaid` يستثني الطلاب الذين عليهم متأخرات لتجنّب التداخل مع `HasOverdue`. **PDF عبر QuestPDF 2024.12.3:** License Community Interlocked guard مرة لكل عملية، Tajawal Regular/Medium/Bold مُسجَّلة من pack URIs، `ContentFromRightToLeft()` على كل صفحة، A4 portrait للطلاب/الحضور/الرسوم و**A4 landscape للدرجات** بسبب أعمدة المواد الديناميكية + تقليل الخط تلقائياً (10→9→8pt) عند > 5/8 مواد، Status pills ملوّنة (Success/Danger/Warning soft) مع MoneyFormatter للعملة. **FlowDocument للمعاينة والطباعة:** 4 وثائق + `ReportDocumentStyle` مشترك (Palette/CreateA4Document/HeaderCell/BodyCell/TotalCell) — نفس قواعد RTL Tajawal Navy/Teal/Muted من ReceiptDocument/StatementDocument، Empty state «لا توجد بيانات لعرضها» عند صفر صفوف، Marks landscape بـ ColumnWidth ≈ 1190 + خلية حالة ملوّنة. PrintPreviewWindow و PrintService مُعاد استخدامها كما هي. **Excel عبر ClosedXML:** `ExcelService` مُمدَّد بـ 4 خرائط (`ExportStudentsReportAsync`/`ExportAttendanceReportAsync`/`ExportMarksReportAsync`/`ExportFeesReportAsync`)، `RightToLeft=true` على كل ورقة، رأس مُجمَّد 3 صفوف (Title + Filter line + Column headers)، Banded rows، تنسيق العملة `#,##0.00 \"ر.س\"`، النسب كـ `0.0%`، Auto-fit مع حدود min/max، Task.Run لتجنب حجب الـ UI. **الصلاحيات:** الـ Hub والـ Sidebar يفلتران على `Permission.ManageReports`، وكل أمر داخلي في الـ 4 دايلوجات يتحقق دفاعياً قبل تشغيل العملية. **Async + CanExecute:** كل العمليات `async`/`await` + `Task.Run` للـ I/O الثقيل + `CanRun()` تربط CommandsCanExecuteChanged بـ IsBusy في كل الـ 4 dialog VMs (لا double-click). **Seeder غني:** إضافة ~900 سجل حضور (30 يوماً دراسياً Sun–Thu، توزيع 90/5/3/2 حاضر/غائب/متأخر/إجازة) + ~513 درجة (6 مواد × 3 امتحانات × 30 طالباً، 5% بدون درجة لإظهار حالات «غير مكتمل» الواقعية) — Random deterministic per-student-subject-exam، Idempotent عبر `AnyAsync` guard، AddRange واحد + SaveChanges واحد، ضمن نفس ExecutionStrategy. **DataTemplates** يربط `Nasag.ViewModels.Pages.Reports.ReportsViewModel` بـ `ReportsView` (alias `reportsVm`/`reportsViews`)، الـ Placeholder القديم مُزال، `Nasag.ViewModels.Pages.ReportsViewModel` المحذوف من `PageViewModel.cs`. **التدقيق النهائي:** 0 Blockers، 4 Major + 6 Minor — أُصلحت: (1) Attendance history-truth filter، (2) Marks IsAbsent semantic، (3) Unpaid/HasOverdue overlap، (4) CanExecute في 3 dialog VMs، (5) TextAlignment.Left → Right في Students summary، (6) `→` arrow → «من X إلى Y» في وصف الحضور. **دورة Polish بعد screenshots من المستخدم:** (أ) **إصلاح خطأ المعاينة الجذري** — `DocumentViewer` لا يقبل FlowDocument (يقتصر على FixedDocument/FixedDocumentSequence)؛ استُبدل بـ `FlowDocumentReader` في `PrintPreviewWindow.xaml` (يدعم FlowDocument أصلياً) مع إخفاء الـ ToolBar الداخلي عبر Visual-Tree walk في `Loaded` لتجنّب التعارض مع شريط الأدوات المخصص. (ب) `PrintPreviewWindow.xaml.cs` لم يعد يُغطّي أبعاد الصفحة بقوّة — `if (double.IsNaN(...))` يحترم الأبعاد التي تحدّدها وثيقة FlowDocument نفسها (يفتح الباب لـ landscape). (ج) **تحويل كل التقارير إلى A4 Landscape** — `ReportDocumentStyle.CreateA4Document(p, landscape: true)` افتراضياً (1122×793 @ 96 DPI)، الـ 4 PDFs تستخدم `PageSizes.A4.Landscape()`، و`NewStarTable(p, weights[])` يستبدل ConstantColumn بـ `GridLength(weight, GridUnitType.Star)` بأوزان مقترنة بالمحتوى (Students 4/8/18/7/14/7/18/12/12، Attendance 4/7/14/11/8/6/6/6/6/6/26، Fees 4/7/14/10/6/16/10/10/10/5/8، Marks 3/5/12 + 8/مادة + 6/5/7/8). (د) **إصلاح كسر النص العربي في خلايا PDF** — `PdfTheme.BodyCell` افتراضي 9pt + LineHeight=1.15 + Padding=3 (بدل 10pt السابقة)، الـ Headers 10pt Bold + LineHeight=1.1 لعناوين بسطرين نظيفة، إزالة `WrapAnywhere` (مُهملة في QuestPDF 2024.3+ ويتعامل الـ layout engine معها تلقائياً)، وموازنة Marks auto-scale عند >8 مواد إلى 8pt. (هـ) FlowDocument body الافتراضي 11→10pt لمطابقة كثافة الـ PDF. Build بعد كل دورة: 0/0. |
| Phase 12 — Settings & Backup | ✅ Completed | 2026-05-21 | 2026-05-21 | مرحلة كاملة لإعدادات المدرسة + إدارة المستخدمين والأدوار + النسخ الاحتياطي والاسترجاع، نُفّذت كوحدة واحدة عبر دورة متعددة الوكلاء (3 وكلاء Explore متوازين للاستطلاع + 3 وكلاء تنفيذ متوازين بفصل صارم للملفات + وكيل تدقيق نهائي مستقل + إصلاحات Blockers). **إعدادات المدرسة:** `ISettingsRepository`/`SettingsRepository` (تحميل/حفظ سجل واحد، إدارة السنوات الدراسية، رفض حذف السنة الحالية أو المرتبطة بشعب/امتحانات/خطط رسوم)، `SettingsViewModel` بنمط Editor كامل (Action Bar علوي بـ حفظ/إلغاء + Ctrl+S/Esc، 3 بطاقات: بيانات المدرسة مع Logo dropzone قابل للنقر كاملاً عبر `IFileService.PickImage`+`ReadAllBytesAsync`+`CanDisplayImage` validation وعرض عبر `BytesToImage` converter، السنة الدراسية الحالية بـ `SearchableComboBox` + قائمة inline بأكشن edit/delete + BubbleButton لإضافة سنة، التفضيلات العامة)، `AcademicYearDialog` بنمط PaymentDialog القياسي (borderless RTL + drag header + error banner). إضافة عمود `LogoBytes varbinary(max)` على `SchoolSettings`. **المستخدمون والأدوار:** `IUsersRepository`/`UsersRepository` بصلاحية `Permission.ManageUsers` دفاعية في كل دالة كتابة، CreateAsync ينشئ مستخدم جديد مع BCrypt hash + تحقق uniqueness للـ Username case-insensitive، UpdateAsync لا يلمس PasswordHash أو Username أبداً، ResetPasswordAsync للأدمن، ChangeOwnPasswordAsync يتحقق من كلمة المرور القديمة قبل التغيير، SetActiveAsync + DeleteAsync يرفضان: (أ) تعطيل/حذف النفس، (ب) آخر مدير في النظام (تعريف "مدير" = صلاحية ManageUsers)، (ج) حذف مستخدم لديه Payments/BackupLogs. UpdateRolePermissionsAsync يقفل آخر دور admin من تجريد ManageUsers. توسعة `IAuthService` بـ `ChangeOwnPasswordAsync`، constructor جديد `(IDbContextFactory, IUsersRepository, ICurrentUserService)`. **UsersView** بنمط StudentsView: header FlowDirection swap، toolbar صف واحد (SearchableComboBox للأدوار + ActiveOnly checkbox + بحث + Refresh/ClearFilter بأيقونات مختلفة + ManageRoles button)، DataGrid (#/FullName/Username/Role/IsActive pill/CreatedAt/LastLoginAt/Actions) + FAB سفلي-أيمن داخل `FlowDirection=LeftToRight` Grid + KeyBindings (Ctrl+N/F5/Delete/Ctrl+F). 3 دايلوجات borderless: `UserEditorDialog` (Username معطّل في Edit، Password+Confirm فقط في Add عبر PasswordChanged handlers في code-behind)، `PasswordResetDialog` (validation matching + length)، `RoleEditorDialog` (12 صلاحية بأسماء عربية كاملة مع قفل ManageUsers لآخر admin role). **النسخ الاحتياطي:** `BackupKind { Backup=0, Restore=1 }` enum جديد + عمود `Kind int` على `BackupLogs` بافتراضي `Backup`. `IBackupService`/`BackupService` يعمل عبر `Microsoft.Data.SqlClient.SqlConnection` مباشرة (EF Core لا يستطيع تشغيل BACKUP/RESTORE): اسم القاعدة من `SqlConnectionStringBuilder.InitialCatalog` بعد تحقق `IsSafeDatabaseName` (whitelist letters/digits/_/-/space ≤128 char) لمنع injection في bracket-quoting، CommandTimeout=0 للعمليات الطويلة، BACKUP يحاول `WITH FORMAT, INIT, COMPRESSION` ثم يرجع بدون COMPRESSION عند SqlException 1844 (LocalDB لا يدعم الضغط)، المسار يمر دائماً عبر SqlParameter `@path`. RESTORE: connection على `master` مع `Pooling=false` + `MultipleActiveResultSets=false` لمنع جلسة pooled قديمة، تسلسل: `ALTER DATABASE [db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE` → `RESTORE … WITH REPLACE` → `SET MULTI_USER` في try/finally (يضمن إعادة الـ DB لـ MULTI_USER حتى عند الفشل). **سجل تدقيق Restore لا يُكتب في DB** لأن RESTORE WITH REPLACE يستبدل القاعدة بالكامل بمحتوى الـ .bak فيمحو أي صف أُضيف قبله؛ السجل الوحيد المعنوي هو صف Backup الذي أنتج الـ .bak. بعد نجاح RESTORE يعرض VM نافذة Success ثم `Application.Current.Shutdown(0)` لإعادة تشغيل نظيف (DbContext state + identity columns لا يمكن استعادتها بأمان in-process). `IBackupsRepository`/`BackupsRepository` بـ projection + AsNoTracking + Add/Delete log. **BackupView:** header + toolbar (مجلد قابل للنقر + Refresh + Create primary + Restore secondary بحدّ DangerSoft) + DataGrid (#/Kind pill teal/amber/FilePath/Size بكيلوبايت/CreatedAt relative/CreatedByName/Notes/Actions) + Empty state 64px + F5. `BackupNotesDialog` بنمط Payment standard. 3 converters جديدة (`BytesToFileSizeConverter`، `BackupKindToArabic/Background/Foreground`) محلية للصفحة. `BackupFolder` يُحفظ في `%LOCALAPPDATA%\Nasaq\prefs.json` (افتراضي `%LOCALAPPDATA%\Nasaq\Backups`). فلتر Sidebar يحترم `Permission.ManageSettings`/`ManageUsers`/`ManageBackup` (موجودة سلفاً في `NavigationDescriptor`). EF migration `AddSettingsLogoAndBackupKind` بـ 2 AddColumn فقط (LogoBytes + Kind). **التدقيق النهائي:** 2 Blockers + 8 Major + 8 Minor — أُصلحت Blockers: (1) سجل التدقيق للاسترجاع كان يُحفظ ثم يُمحى → أُزيل ووُضع تعليق توضيحي، (2) تكوين أسماء قاعدة بيانات غير آمن → `IsSafeDatabaseName` whitelist. باقي Major/Minor مُوثّقة في Decisions Log كقرارات مؤجلة (Users pagination، تأكيد عمل SqlParameter في BACKUP/RESTORE على LocalDB، Username uniqueness SARG، LogoPath legacy column). Build 0/0. |
| Phase 14 — Licensing + Velopack Updates + Decompilation Protection | ✅ Completed | 2026-05-22 | 2026-05-22 | مرحلة كاملة نُفّذت كوحدة واحدة عبر دورة متعددة الوكلاء (وكيل بحث ويب + وكيل استكشاف codebase + 4 وكلاء تنفيذ بفصل صارم للملفات + تدقيق نهائي). **حُذفت أولاً** بقايا محاولة سابقة (مجلدات فارغة + bin/obj قديمة) بطلب صريح من المستخدم «احذفهم. ابدأ من الصفر»، ثم بُنيت 3 مشاريع جديدة كاملة. الحل صار يحوي **4 مشاريع**: `Nasag` + `Nasag.Licensing` + `NasaqVendor` + `NasaqPackager`. **`Nasag.Licensing` (Class Library):** `MachineFingerprint` بـ 5 hashes (CPU/Board/BIOS/MachineGuid/MAC) عبر `System.Management 8.0.0` + N-of-M matching (3 من 5) + cache، `EcdsaSigner` P-256 + `KeyImportExport` PEM helpers + `EmbeddedPublicKey` loader، `LicenseFile` JSON DTO + `LicenseSerializer` (canonical: System.Text.Json → JsonNode → recursive ordinal key sort → minified) + `LicenseValidator` (signature → expiry → 3-of-5 machine match) + `LicenseStatus` discriminated record (Trial/Activated/Expired/TamperedClock/MachineMismatch/InvalidSignature/Missing)، `ProtectedStateStore` (DPAPI CurrentUser + OptionalEntropy "Nasaq.Licensing.v1" + atomic `.tmp`+`File.Replace` + `Global\Nasaq.Licensing.State` mutex)، `RegistryMirror` HKCU، `TrialManager` (30 يوم HMAC-bound to fingerprint + restore-from-registry anti-uninstall-reset)، `ClockTamperDetector` (high-watermark UTC + 1h backward slack + kernel32.dll sanity 30 days)، `AntiTamper` (`Debugger.IsAttached` + P/Invoke `CheckRemoteDebuggerPresent` + timing check 200k-iter + assembly SHA-256). Trial و Clock يستخدمان ملفّين منفصلين `trial.dat`/`clock.dat`. **`NasaqVendor.exe` (WPF + SQLite via Microsoft.Data.Sqlite 8.0.10 + Dapper 2.1.35):** أداة المورّد المنفصلة. Borderless RTL 1100×720 بنمط NasaqDialog، 3 شاشات في Navy sidebar (`Customers`/`Licenses`/`KeySettings`). DB في `%LOCALAPPDATA%\NasaqVendor\vendor.db` بـ 3 جداول (`Customers (Code unique)` + `Licenses (FK Customer + MachineHashesJson + LicenseFilePath + Revoked)` + `IssueAudit (LicenseId + Action)`). `IssuerKeyService` يحمي المفتاح الخاص بـ DPAPI في `%LOCALAPPDATA%\NasaqVendor\issuer.key`. `InitialKeySetupDialog` يظهر على أول تشغيل إذا لم يوجد مفتاح: خياران «توليد زوج جديد» / «استيراد مفتاح موجود». 5 دايلوجات borderless لـ Customer Editor/Issue License/Revoke/Audit Log/Initial Setup. `IssueLicenseDialog` يقبل 5 hashes من العميل (multi-line 64-hex validation) + Edition + Features checklist + ExpiresAtUtc اختياري → يبني LicenseFile → يوقّع canonical bytes بـ ECDSA → يحفظ `.naslic` (default `{CustomerCode}-{yyyyMMdd}.naslic`) → يُضيف صف Licenses + IssueAudit. **`NasaqPackager.exe` (WPF):** أداة التحزيم. Borderless RTL 980×640 بـ 3 أزرار `BubbleXxl` (220×120 Teal): «زيادة رقم النسخة» / «تحزيم وإصدار» / «فتح مجلد الإصدارات». `ProjectVersionService` يعدّل `<Version>` في `Nasag.csproj` عبر XDocument (يضيف Element إذا غاب، يزيد patch إذا موجود). `PipelineRunner` بـ `Channel<string>` + `IAsyncEnumerable<string>` يستريم stdout/stderr من `dotnet publish` → optional `obfuscar.console` → `vpk pack` إلى log panel terminal-style (Navy bg + Consolas + auto-scroll). `PackagerSettings` JSON في `%LOCALAPPDATA%\NasaqPackager\settings.json` يحفظ مسارات المشروع/الإصدارات/الأيقونة + Channel + RID + SelfContained. `SettingsDialog` borderless RTL لتعديل الكل. **التكامل في `Nasag` الرئيسي:** `<Version>1.14.0</Version>` + `<ProjectReference>` لـ Nasag.Licensing + `Velopack 0.0.1298` + `<EmbeddedResource>` للمفتاح العام `Resources\issuer.public.key` بـ `LogicalName="Nasag.issuer.public.key"` + Release config `DebugType=none`+`DebugSymbols=false`+`Optimize=true`. **`App.OnStartup`** يستدعي `VelopackApp.Build().Run()` كأول سطر (Velopack hooks تتعامل مع `--veloapp-*` switches قبل WPF init — تم اعتماد هذا المسار بدلاً من `<StartupObject>` لتجنّب التعارض مع WPF source-gen Main). `OnSplashCompleted` يفحص `ILicenseService.GetStatusOnStartup()` بعد `CloseSplash()` وقبل `WireAuthLifecycleAndShowLogin()` — إذا لم تكن الحالة `Activated` أو `Trial` يفتح `LicenseGateWindow` بدل Login. **`ILicenseService` / `LicenseService` Singleton:** facade فوق Nasag.Licensing — يجمع machine hashes مرة، يحمّل `%LOCALAPPDATA%\Nasaq\license.naslic`، يحسب Status، `ActivateAsync(filePath)` ينسخ الملف + يعيد التحقق + يطلق `StatusChanged`، `Deactivate()` يحذف الملف. Exposes `MachineFingerprintBlock` متعدد الأسطر لنسخه. **`IUpdateService` / `UpdateService` Singleton:** يلفّ Velopack `UpdateManager` فوق `SimpleFileSource(DirectoryInfo)` من `IUserPreferencesService.UpdateSourceFolder` (default `%LOCALAPPDATA%\Nasaq\Updates`). `CheckAsync/DownloadAsync(progress)/ApplyAndRestart`. التحقق التلقائي عند بدء التشغيل: fire-and-forget بعد 5 ثوانٍ من دخول MainShell → Toast Info «تتوفر نسخة جديدة» يفتح UpdateWindow. **3 نوافذ borderless RTL:** `LicenseGateWindow` 640×480 (shield ملوّن حسب الحالة + سبب الرفض بالعربية + زرّيْ «تفعيل البرنامج» Primary / «إغلاق» Ghost)، `ActivationWindow` 720×640 wizard 4 خطوات (Welcome/MachineId/UploadLicense/Done) بنمط `SetupWizardWindow` Phase 13 — Step 1 يعرض 5 hashes + ZIP block للنسخ، Step 2 drop zone + paste textarea + preview card، Step 3 confetti + RestartNow؛ Step transitions تستخدم أسماء الـ commands المولّدة الصحيحة (مولّد `[RelayCommand]` يحذف لاحقة `Async` — تم تجنّب الـ Phase 13 landmine)، `UpdateWindow` 480×360 (checking spinner → up-to-date OR new version available → progress bar → ready to restart). **شارة TopBar:** Pill جديد بأيقونة Shield في column 3 من Grid الـ TopBar (قبل بحث + شريحة سنة)، نص يعكس Status (Trial 23 يوم / مفعّل — اسم العميل / منتهي)، لون يعكس Kind (Teal/Success/Warning/Danger). `MainShellViewModel` يحقن `ILicenseService` + يتعرّف `LicenseBadgeText/Kind`. **بطاقتان جديدتان في SettingsView بين بطاقة DB وبطاقة Preferences:** «إدارة الترخيص» (IconCertificate bubble + Status + Customer + Edition + Expiry + 3 أزرار: تفعيل/نسخ رمز الجهاز/إلغاء التفعيل destructive)، «التحديثات» (IconDownload bubble + النسخة الحالية + آخر فحص + زر «التحقق» يفتح UpdateWindow + زر «المصدر…» OpenFileDialog لاختيار مجلد). **`Themes/Icons.xaml`:** أيقونات جديدة `IconShield`/`IconCertificate`/`IconDownload`/`IconPackage`/`IconKey`/`IconCopy`/`IconCheckCircle`. **`Obfuscar.xml` في جذر الحل:** `HideStrings=true`+`UseUnicodeNames=true`+`RenameProperties=true`+`AnalyzeXaml=true` مع `SkipType` للـ XAML-bound ViewModels/Views/Controls + EF Models + Helpers + Converters في Nasag.dll، و skipMethods على Nasag.Licensing.dll للسطح العام المطلوب من Nasag. **`build-installer.ps1` في جذر الحل:** ينفّذ publish + obfuscar + vpk pack بمعاملات صحيحة + يخرج `Releases\Customer\Setup.exe` + يرجع الإصدار من csproj إذا لم يُمرَّر. **`Nasag.slnx`** يحوي الآن 4 مشاريع. **زوج المفاتيح:** تم توليده مرّة عبر مشروع KeyGen مؤقت (حُذف بعد التوليد) — `Nasag/Resources/issuer.public.key` (91 bytes SPKI) + `dev-issuer.private.key` (121 bytes ECPrivateKey) في جذر الحل. **التحقق المستقل:** `dotnet build Nasag.slnx -c Debug` → 0 Warning / 0 Error في ~5s، roundtrip اختبار بـ console مؤقت: `ECDsa.SignData(data, privateKey) → VerifyData(data, signature, publicKey)` ✅ PASS (signature 64 bytes كمتوقع لـ P-256)، فحص `grep AsyncCommand` على `Views/Licensing/*.xaml` → لا تطابق (تم تجنّب Phase 13 landmine)، `App.xaml.cs` يحوي `VelopackApp.Build().Run()` سطر 60 + `_licenseService.GetStatusOnStartup()` سطر 139 + `ShowLicenseGate` سطر 161 + DI registrations سطر 373-374. **حدود الحماية المعترفة:** فك التجميع بـ Obfuscar+HideStrings يردع المخترق العادي لكن `dnSpyEx + de4dot` يستطيع كسر التشويش في ساعة — القفل الفعلي هو توقيع ECDSA P-256 (لا يمكن تزوير ترخيص بدون المفتاح الخاص للمورّد). أدوات مجانية بالكامل: Velopack MIT + Obfuscar MIT + System.Management + ProtectedData. لا .NET Reactor، لا Authenticode EV. Build 0/0 على المشاريع الـ 4 في Debug و Release. |
| Phase 13 — Polish + Splash + Setup Wizard + Multi-Connection | ✅ Completed | 2026-05-21 | 2026-05-21 | المرحلة النهائية، نُفّذت كوحدة واحدة عبر دورة متعددة الوكلاء (3 وكلاء Explore متوازين للاستطلاع + 3 وكلاء تنفيذ متوازين بفصل صارم للملفات + وكيل تدقيق نهائي مستقل + إصلاح 4 Major). **Splash Screen:** نافذة احترافية borderless RTL 520×380 (نمط NasaqDialog/PaymentDialog: WindowStyle=None + AllowsTransparency + DragMove + Card 14px CornerRadius + Navy shadow #1B3A57)، تحوي شعار «ن» في bubble تيل 80×80 + عنوان «نَسَق» 28pt + سبيرنر inline (نفس geometry من LoadingOverlay، Ellipse + Arc rotating storyboard) + نص حالة ديناميكي. **SplashViewModel** بـ CommunityToolkit.Mvvm، يحقن `IDatabaseInitializer`/`IConnectionStringProvider`/`IErrorReporter`، يفعّل status flow: «جاري التحضير» → «جاري التحقق من الاتصال» → «جاري التحقق من التحديثات» → «جاري تحديث قاعدة البيانات» → «جاري تحميل البيانات الأولية» → «جاهز». على نجاح: يطلق `Completed` بـ `SplashResult(Success)`. على `CannotConnect`: يطلق `Completed` بـ `CannotConnect` (App يفتح المعالج، بدون فلاش أحمر). على `MigrationFailed/SeedFailed/Unknown`: يحوّل الـ Splash إلى وضع خطأ (Danger soft banner + رسالة + زرَّيْ «إعادة المحاولة» و«فتح معالج الإعداد»). كل تحديثات UI marshalled على Dispatcher. **First-Run Setup Wizard:** نافذة borderless 640×640 بـ 5 خطوات (Welcome → Choose mode → Details → Test → Finish) مع stepper مرئي بنقاط/خطوط تتلوّن Teal للمكتمل و BorderStrong للقادم، و3 خيارات RadioButton-as-cards (LocalDB / SQL Server / Custom). **SetupWizardViewModel:** يحقن `IConnectionStringProvider`/`IConnectionTester`/`IErrorReporter`، يحتوي 5 خطوات + ConnectionMode enum + حقول Server/DatabaseName/Username/Password/UseWindowsAuth/CustomConnectionString، يبني سلسلة الاتصال عبر `SqlConnectionStringBuilder` (TrustServerCertificate=true + MultipleActiveResultSets=true لمطابقة appsettings.json). **CanGoNext** يفرض validation متعدد الطبقات: الخطوة 2 (Details) ترفض التقدم بدون ServerName في وضع SqlServer/DatabaseName دائماً/CustomConnectionString في Custom mode؛ الخطوة 3 (Test) تتطلب `CanProceedFromTest=true`؛ الخطوة 4 (Finish) تتعطّل أثناء `IsFinishing` لمنع Double-click. `NextStepAsync` (RelayCommand async) يستدعي FinishAsync await-fully على الخطوة الأخيرة. **`IConnectionTester` + `ConnectionTester`** عبر `Microsoft.Data.SqlClient.SqlConnection` مباشرة (نفس نمط BackupService): `TestAsync` يستنسخ SqlConnectionStringBuilder ويستهدف `master` بـ `Pooling=false`+`ConnectTimeout=8`، ثم يستعلم `sys.databases WHERE name=@name` بـ `SqlParameter` (لا concat نص أبداً)، يرجّع نتيجة Arabic + DatabaseExists flag. `CreateDatabaseAsync` يستخدم نفس `IsSafeDatabaseName` whitelist من `BackupService` (letters/digits/`_`/`-`/space ≤128) قبل bracket-quoting، CommandTimeout=0. كل الرسائل عربية + التفاصيل التقنية في `Details` منفصل. **PasswordBox handling** عبر code-behind (`OnPasswordChanged → vm.Password = pb.Password`) لأن WPF لا يكشف Password كـ DependencyProperty لأسباب أمنية، Step 4 يخفي كلمة المرور عبر `MaskPassword` (`b.Password = "***"`). **`IConnectionStringProvider` + `ConnectionStringProvider`:** يقرأ `%LOCALAPPDATA%\Nasaq\connection.json` (override per-machine، JSON `{ "DefaultConnection": "..." }`) ويسقط على appsettings.json، ثم على LocalDB default ثابت إن كان كلاهما فارغاً (يضمن `Current` لا يكون فارغاً أبداً → الـ host يبني دائماً والـ splash تتولى المسار من هناك). Thread-safe (lock حول I/O)، Save ينشئ المجلد ويكتب pretty JSON، ClearOverride يحذف الملف ويعيد الـ Refresh. corrupt JSON يسقط بصمت على appsettings (تعليق توضيحي). `Source` يكشف "UserOverride" / "AppSettings" / "Default" — لا يحوي كلمة المرور. **DbContextFactory** يبقى يقرأ `Current` مرة واحدة عند بناء الـ Host (no runtime swap)؛ تطبيق الإعدادات الجديدة يتطلب إعادة تشغيل (متوقع). **App.xaml.cs orchestration:** OnStartup يبني Host → يصل global exception handlers → يعرض SplashWindow أول شيء كـ MainWindow → يشترك على `splashVm.Completed` → يطلق `RunInitAsync` fire-and-forget. `OnSplashCompleted` marshalled على Dispatcher: Success → CloseSplash + WireAuthLifecycleAndShowLogin (الـ flow الموجود سابقاً يبقى كما هو)؛ CannotConnect → CloseSplash + ShowSetupWizard (resolves wizard via DI، ShowDialog، on true → success NasaqDialog ثم RestartApplication، on false → Shutdown(0))؛ TerminalError → splash يبقى مرئي في error state. `RestartApplication` = `Process.Start(MainModule.FileName)` ثم `Shutdown(0)`. كل registrations سابقة محفوظة، أضيفت 5 lines DI فقط لـ Phase 13 (`IConnectionStringProvider`/`IConnectionTester`/`SplashViewModel`/`SetupWizardViewModel`/`SetupWizardWindow`). **Settings entry point:** بطاقة جديدة «اتصال قاعدة البيانات» في `SettingsView.xaml` بين بطاقة السنة الدراسية وبطاقة التفضيلات، بـ IconBackup bubble + heading + subtitle + SecondaryButton «إعادة إعداد قاعدة البيانات»، مربوط بـ `OpenDatabaseSetupCommand`. `SettingsViewModel` يحقن `IServiceProvider` (cleaner من Application.Current cast) ويستخدمه لتحضير الـ wizard، وعلى نجاح يعرض NasaqDialog Info «أعد تشغيل البرنامج لتطبيق التغييرات». **Polish:** (أ) `ErrorReporter` يحتفظ بـ `MessageBox.Show` كـ fallback وحيد قبل Dispatcher (`IDialogService` يحتاج WPF context)، مع `MessageBoxOptions.RtlReading | RightAlign` على المسارين + تعليق توضيحي. (ب) `Themes/Colors.xaml`: إضافة `OverlayDarkBrush` (Black @0.2 ≈ `#33000000` السابق) جوار `OverlayBrush` الموجود مسبقاً. (ج) `ErrorWindow.xaml`: `#33000000` → `{StaticResource OverlayDarkBrush}`. (د) 4 شاشات (Attendance/Marks/Results/Subjects): `Background="#AAFFFFFF"` → `{StaticResource OverlayBrush}`. (هـ) `README.md` عربي (~165 سطر، 12 قسم: المميزات/المتطلبات/التثبيت/بيانات الدخول/معالج الإعداد/النسخ الاحتياطي/التقارير/الصلاحيات/استكشاف الأخطاء/مسارات البيانات/بنية المشروع/الترخيص). **التدقيق النهائي المستقل:** 0 Blockers + 4 Major + 9 Minor — أُصلحت كل الـ 4 Major: (1) إضافة validation gate للخطوة 2 في `CanGoNext` مع NotifyPropertyChangedFor على ServerName/DatabaseName/CustomConnectionString/Mode، (2) `ConnectionStringProvider` لا يرجّع فارغاً أبداً (LocalDB default fallback) مما أزال boot-time Shutdown path، (3) إزالة `Command="{Binding NextStepCommand}"` الخاطئ من RadioButton الـ LocalDB، (4) إضافة `IsFinishing` flag + تحويل `NextStep` إلى async + double-click guard على FinishAsync. الـ 9 Minor مُوثّقة كقرارات مؤجلة (password كـ string بدلاً من SecureString، ConnectTimeout 8s vs 15s، corrupt JSON silent fallback، RestartApplication exception silence — كلها مقبولة لـ Phase 13). Build 0/0 قبل وبعد إصلاحات Major. **توسعة Multi-Connection (نفس الجلسة):** بناءً على طلب صريح من المستخدم لتمكين (أ) ظهور المعالج عند أول تنصيب، (ب) دعم إنشاء قاعدة جديدة أو الاتصال بموجودة، (ج) كشف الخوادم وعرض قواعد البيانات تلقائياً، (د) عرض القاعدة الحالية في شاشة Login مع dropdown بالقواعد المحفوظة وخيار «إضافة قاعدة جديدة» يفتح المعالج. **Backend (Wave 1):** `SavedConnection { Id, DisplayName, ConnectionString, CreatedAt, LastUsedAt }` + `IConnectionRegistry`/`ConnectionRegistry` (يستبدل `IConnectionStringProvider`): تخزين متعدد القواعد في `%LOCALAPPDATA%\Nasaq\connections.json` بشكل `{ ActiveConnectionId, Connections[] }`، Migration صامتة من ملف Phase 13 القديم (`connection.json` → SavedConnection بـ DisplayName «قاعدة البيانات الأساسية» ثم حذف الملف القديم)، Thread-safe (lock حول كل I/O)، snapshot مُخبَّأ في `_allSnapshot` يُحدَّث بـ `RefreshSnapshotNoLock` بعد كل mutation فيستقر للـ WPF bindings ولا يُعاد بناؤه عند كل قراءة، Changed event يُطلق خارج الـ lock لتجنّب re-entrancy، Remove يرشّح أول اتصال متبقٍ كـ Active إذا حُذف النشط، ActiveConnectionString يضمن قيمة غير فارغة دائماً (Saved → AppSettings → LocalDB sentinel). `IServerDiscoveryService`/`ServerDiscoveryService` يستخدم `Microsoft.Data.Sql.SqlDataSourceEnumerator.Instance.GetDataSources()` مغلَّفاً بـ `Task.WhenAny` + timeout 6 ثوانٍ، يُرجّع DiscoveredServer record (DisplayName, ConnectionTarget, IsLocal) مع 3 fallback entries ثابتة (LocalDB، .، .\SQLEXPRESS)، dedup case-insensitive، locals أولاً ثم alphabetic، لا يرمي أبداً. `IConnectionTester.ListDatabasesAsync` جديد: يستهدف master ويستعلم `sys.databases WHERE database_id > 4 AND state_desc = 'ONLINE' ORDER BY name`، يرجّع قائمة فارغة على الفشل (لا exception). `IApplicationRestarter`/`ApplicationRestarter` خدمة Singleton جديدة بـ `RestartNow()` يضمن Dispatcher marshal + try/catch على Process.Start مع IErrorReporter + Shutdown(0) في finally — استُبدل `App.RestartApplication()` الخاص به. **Wizard redesign (Wave 2A):** إعادة كتابة كاملة لـ `SetupWizardViewModel` + `SetupWizardWindow.xaml/.cs` بتدفّق 5 خطوات جديد user-friendly: (الخطوة 0) Welcome + اختيار `WizardIntent` (CreateNew/UseExisting) كبطاقتي RadioButton كبيرتين، `HasPickedIntent` يُهيَّأ إلى true ليتطابق مع الـ default المُعلَّم بصرياً (CreateNew) ويتجنّب تجربة «راديو مُعلَّم + Next مُعطَّل». (الخطوة 1) Server picker `SearchableComboBox` مربوط بـ `IServerDiscoveryService.DiscoverAsync` (يُسخَّن من `OnWindowLoaded` ليكون جاهزاً قبل وصول المستخدم) + زر «تحديث القائمة» (BubbleButton) + TextBox manual override (`ServerNameInput`، له الأولوية في `EffectiveServer` على `SelectedServer.ConnectionTarget`)، Auth mode بـ RadioButton (Windows/SQL) + Username TextBox + PasswordBox bridge، يستعيد كلمة المرور المخزّنة في الـ VM عند Back→Next عبر `Loaded` و `IsVisibleChanged` handlers (PasswordBox لا تربط Password كـ DP). (الخطوة 2) database picker context-aware: في UseExisting يُملأ من `LoadDatabasesAsync` (يُطلق عند دخول الخطوة + زر تحديث، Arabic error banner + manual TextBox fallback عند الفشل)، في CreateNew TextBox مع caption whitelist. (الخطوة 3) زر اختبار single نصّه ديناميكي عبر `TestActionButtonText` («اختبار الاتصال» → «إنشاء قاعدة البيانات» في CreateNew بعد test ناجح والـ DB مفقودة → «إعادة الاختبار»)، `RunTestActionAsync` يستخدم `shouldCreate` flag للتفريق بين المسارات + re-test تلقائي بعد CreateDatabase ناجح، UseExisting يتطلب `Success && DatabaseExists` لتفعيل Next. (الخطوة 4) DisplayName TextBox (default = `"{DatabaseName} ({ShortServerName})"`) + summary card + زر Footer «حفظ وإعادة التشغيل»، Finish يستدعي `_registry.Add(displayName, cs)` ثم `SetActive(entry.Id)` ثم `RequestClose(true)`. كل الـ chrome borderless RTL 720×720 بنفس نمط Phase 13 + stepper بـ 5 نقاط/4 خطوط ملوّنة Teal للمكتمل و BorderStrong للقادم + DataTriggers على `IsStepNActive`. **Login picker (Wave 2B):** `LoginView.xaml` يحتوي connection bar داخل بطاقة الدخول (أعلى الـ Error Banner) بـ IconBackup + caption «قاعدة البيانات:» + SearchableComboBox مربوط بـ `Registry.All` و `SelectedConnection` (TwoWay) + GhostButton «إضافة» مع IconAdd، يختفي تلقائياً عند `Registry.IsEmpty` عبر `InverseBoolToVisibilityConverter`. `LoginViewModel` يحقن `IConnectionRegistry`/`IApplicationRestarter`/`IServiceProvider`/`IDialogService`/`IErrorReporter`/`IToastService`، يعرّف `Registry` property + `AvailableConnections` snapshot + `[ObservableProperty] SavedConnection? selectedConnection` (مُهيَّأ عبر backing field لتجنّب setter cascade)، `partial OnSelectedConnectionChanged` يستدعي `_dialogs.ConfirmAsync` على التبديل ثم `_registry.SetActive(id)` ثم `_restarter.RestartNow()`، مع `_isSwitchingProgrammatically` guard في try/finally لتجنّب infinite loops عند Cancel revert. `AddConnectionCommand` يحلّ `SetupWizardWindow` + `SetupWizardViewModel` من DI، يفتح ShowDialog، على true → restart فوري (المعالج أكد التشغيل بنفسه). `_registry.Changed` subscription موثَّق كـ "small leak window" مقبول لأن LoginVM Transient + قصير العمر. **Settings entry محسَّن:** بدلاً من ShowInfoAsync بعد المعالج، الآن `ConfirmAsync("تم حفظ إعدادات الاتصال", "هل تريد إعادة التشغيل الآن؟")` → On Yes: `_restarter.RestartNow()`، يوفّر تشغيل بنقرة واحدة. **التدقيق المستقل:** 0 Blockers + 6 Major + 7 Minor — أُصلحت 3 Major + 2 Minor: (1) `All` snapshot caching يستقر للـ bindings، (2) `_hasPickedIntent = true` افتراضياً لمطابقة البصري، (3) `Debug.WriteLine` في catch blocks للـ Load + Migration للتشخيص، (4) إزالة "(InitialCatalog)" المسرَّب من رسالة عربية، (5) PasswordBox restore بـ Loaded + IsVisibleChanged handlers. باقي Major مقبولة: refresh button silent guard (defensive)، DatabaseName overwrite UX (مقبول)، orphan discovery task (VM-level guard كافٍ). الـ 7 Minor مقبولة (LoginVM transient leak documented، ServerDiscovery orphan probe acceptable إلخ). Build 0/0 في كل من Wave 1، Wave 2 (دمج Y+Z)، وبعد إصلاحات التدقيق. **الملفات النهائية:** 4 services جديدة (SavedConnection، IConnectionRegistry/ConnectionRegistry، IServerDiscoveryService/ServerDiscoveryService، IApplicationRestarter/ApplicationRestarter) + extension لـ IConnectionTester (ListDatabasesAsync) + إعادة كتابة كاملة لـ Setup wizard (VM + XAML + code-behind) + LoginView/LoginViewModel + SettingsViewModel + App.xaml.cs، حذف `IConnectionStringProvider`/`ConnectionStringProvider` (لم يعد لهما مستخدم). **Fresh DB + Admin Setup (طلب لاحق في نفس الجلسة):** المستخدم طلب أن تكون قاعدة البيانات المنشأة عبر CreateNew فارغة fresh + المعالج يجمع بيانات المدير أثناء الإعداد. **التنفيذ:** (أ) `PendingAdminSetup` DTO + `IPendingAdminSetupStore`/`PendingAdminSetupStore` تخزّن بيانات المدير (FullName/Username/Password) مشفّرة بـ DPAPI (`DataProtectionScope.CurrentUser`) في `%LOCALAPPDATA%\Nasaq\pending-admin.dat`؛ Thread-safe (lock حول I/O)؛ Save يكتب بـ `JsonSerializer` → `ProtectedData.Protect` → `File.WriteAllBytes`؛ `ReadAndClear` تعيد ثم تحذف ذرّياً مهما حدث (corrupt → null + delete، لا تَرمي أبداً)؛ Singleton DI. حزمة `System.Security.Cryptography.ProtectedData 8.0.0` أُضيفت لـ `.csproj`. (ب) `DbSeeder` refactored لسطح ثابت + 4 methods داخلية: `SeedIfEmptyAsync` (idempotent guard أولاً ثم `ReadAndClear`)، `SeedRolesAsync` (4 أدوار بصلاحياتها المشتركة بين الوضعين)، `SeedAdminAsync(pendingAdmin)` (BCrypt workFactor=11 + IsActive=true + RoleId=Admin)، `SeedSchoolPlaceholderAsync` (`NameAr="مدرستي"` + `CurrentAcademicYearId=null` فقط لـ minimal)، `SeedFullDemoAsync` (المنطق السابق كاملاً للتطوير). Branch: pending موجود → `SeedMinimalAsync` (Roles + Admin من البطاقة + School placeholder) ضمن ExecutionStrategy+Transaction؛ pending=null + DB فارغة → fallback إلى full demo seed (يحفظ تجربة المطوّر)؛ DB ممتلئة → no-op (idempotent). **CRITICAL**: idempotency guard `if Users.Any → return` يسبق `ReadAndClear`، فلا يستهلك payload على قاعدة لها بيانات سلفاً (يمنع فقدان صامت). (ج) Wizard Step 4 تستقبل قسماً جديداً «حساب المدير الأول» مرئياً فقط في `IsCreateNew` (Visibility binding على `BoolToVisibility`)، 4 حقول: FullName/Username (default "admin")/Password/ConfirmPassword عبر PasswordBox bridges + Loaded restore handlers. Validation متعدد الطبقات: `AdminValidationError` computed يفحص بالترتيب (FullName/Username/Password/Length≥6/Match) ويعيد null للـ UseExisting، `CanGoNext` Step 4 يربط `IsCreateNew && AdminValidationError != null` كـ block، Inline banner Danger-soft يعرض الرسالة. `[NotifyPropertyChangedFor]` على الأربعة + Intent ليُحدَّث الـ validation فوراً عند تبديل الـ Mode. (د) `FinishAsync` يحفظ pending payload **قبل** registry.Add، إذا فشل Save لا يتقدم (التطبيق لن يُنشئ قاعدة بمدير غير معروف)؛ إذا نجح Save ثم فشل registry.Add يستدعي `ReadAndClear` لتنظيف الملف المؤقت لئلا يستهلكه seeder على قاعدة لاحقة لا علاقة لها (إصلاح Major من التدقيق). **التدقيق المستقل:** 0 Blockers + 1 Major (أُصلح: orphan pending file on registry failure) + 6 Minor (مقبولة، password defensive fallback في seeder غير قابل للوصول عملياً). Build 0/0. **ملاحظتان أخيرتان للمستخدم:** (أ) إزالة بطاقة «حساب تجريبي: admin / admin123» من LoginView (لم تعد ذات معنى بعد أن صار المستخدم يحدّد بياناته بنفسه في المعالج). (ب) «تذكّرني» الآن يحفظ كلمة المرور أيضاً (لا فقط اسم المستخدم): `UserPreferences.SetRememberedPassword(plaintext)` يُشفِّر بـ DPAPI (CurrentUser scope) → Base64 ويحفظ في `RememberedPasswordProtected`؛ `GetRememberedPassword()` يفك التشفير، ويرجّع null عند الفشل/الفساد (مثل تغيير ملف تعريف Windows). LoginViewModel constructor يحمّل الـ password المُشفَّر إلى backing field `_password`. LoginView code-behind في `OnViewLoaded` يحقن قيمة الـ VM إلى `PasswordField.Password` (لا يمكن binding مباشر لـ Password DP لأسباب أمنية) ثم ينقل التركيز لزر تسجيل الدخول فيكفي Enter للدخول دون كتابة. LoginAsync يحدّث الـ prefs: عند تفعيل تذكّرني يحفظ الاسم والكلمة، عند تعطيله يمحو كليهما. **3 إصلاحات حرجة بعد تجريب المستخدم الفعلي:** (1) **Blocker حرج** — كل أوامر المعالج المربوطة في XAML (`NextStepAsyncCommand`, `DiscoverServersAsyncCommand`, `LoadDatabasesAsyncCommand`, `RunTestActionAsyncCommand`) كانت تشير لأسماء غير موجودة لأنّ مولّد `[RelayCommand]` من CommunityToolkit.Mvvm **يحذف لاحقة `Async`** عند توليد اسم الـ command (`NextStepAsync` method → `NextStepCommand` property). نتيجة: WPF يكتب binding error صامت ولا تستجيب أي نقرة في المعالج (Next/Test/Refresh)، فيظن المستخدم أنّ المعالج جامد. أُصلحت الأربعة في `SetupWizardWindow.xaml`. (2) **Blocker** — `SplashViewModel.RunInitAsync` كان يضيف `ActiveConnectionString` تلقائياً للسجل عند Success حتى عندما `Source == "Default"` (LocalDB sentinel، لا appsettings ولا user override)، فيخفي المعالج عن المستخدم في كل التشغيلات اللاحقة على جهاز فيه LocalDB مثبَّت. الإصلاح: شرط إضافي `_registry.Source != "Default"` قبل الـ Add، و`MarkActiveUsed` يُستدعى فقط عند وجود سجل. (3) **Blocker** — `App.ShowSetupWizard` كان يستدعي `Shutdown(0)` فوراً عند إلغاء المعالج (أو إغلاقه بـ ×)، فيختفي التطبيق صامتاً بنقرة خطأ واحدة. الإصلاح: تحويل المسار إلى `while (true)` loop يفتح NasaqDialog Confirm («لم يتم إعداد قاعدة بيانات. هل تريد إغلاق البرنامج الآن؟» مع زرَّيْ «إغلاق البرنامج» و«العودة للمعالج»)، فالمستخدم يحصل على فرصة واضحة لاستئناف الإعداد أو الخروج بإدراك تام. Build 0/0 بعد الإصلاحات. |

---

## 9. Decisions Log

| التاريخ | القرار | السبب |
|--------|--------|------|
| 2026-05-15 | اعتماد .NET 8 WPF | الإطار الموجود مسبقاً في `Nasag.csproj` ويلبي كل متطلبات WPF + DI الحديثة |
| 2026-05-15 | اعتماد MVVM مع CommunityToolkit.Mvvm | تقليل boilerplate عبر Source Generators، مدعوم من Microsoft |
| 2026-05-15 | EF Core 8 + SQL Server (LocalDB في التطوير) | متطلب المستخدم صريح؛ LocalDB يبسّط التشغيل أثناء التطوير |
| 2026-05-15 | اعتماد Tajawal مع تضمينه في Assets/Fonts | متطلب صريح وضمان توفر الخط على أي جهاز |
| 2026-05-15 | استخدام Repository Pattern فوق EF Core | فصل واضح ويسهّل الاختبار |
| 2026-05-15 | تأجيل اختيار مكتبة Chart إلى Phase 5 | تجنب التزام مبكر؛ LiveCharts2 هو المرشح الأول |
| 2026-05-15 | تأجيل اختيار مكتبة PDF إلى Phase 11 | QuestPDF مرشح قوي لدعم RTL العربي |
| 2026-05-15 | استبعاد LMS / تطبيق ولي أمر / محاسبة كاملة | متطلب المستخدم الصريح بحصر النطاق |
| 2026-05-15 | تثبيت Tajawal محلياً في `Assets/Fonts` (Regular/Medium/Bold من Google Fonts) | ضمان توفر الخط دون اعتماد على نظام المستخدم — Build Action: Resource |
| 2026-05-15 | استخدام CommunityToolkit.Mvvm `ObservableObject` و`RelayCommand` بدلاً من Helpers يدوية | تقليل boilerplate، Source Generators رسمية من Microsoft؛ ألغى الحاجة لـ `Helpers/BaseViewModel.cs` |
| 2026-05-15 | اعتماد Microsoft.Extensions.Hosting (Generic Host) لإدارة DI والتكوين | نمط حديث وموحّد، يفتح الباب لاحقاً لـ Logging وOptions Pattern بدون تغيير |
| 2026-05-15 | حذف `StartupUri` من `App.xaml` وإنشاء `MainWindow` يدوياً داخل `OnStartup` | لتمكين حقن `MainViewModel` في `DataContext` عبر DI قبل الإظهار |
| 2026-05-15 | تأجيل أنماط Buttons/Inputs/DataGrid/Cards التفصيلية إلى Phase 2 | Phase 1 تركز فقط على الأساس؛ الأنماط جزء أصيل من "Design System and Main Shell" |
| 2026-05-15 | كل commit بحساب المطوّر فقط، **بدون** أي سطر Co-Authored-By وبدون عمل commit تلقائي | متطلب المستخدم الصريح؛ الوكيل يطلب الإذن قبل كل commit ويستخدم `git config` المحلي كما هو |
| 2026-05-15 | اعتماد `Database.MigrateAsync()` ديناميكياً بدلاً من `EnsureCreated` أو سكربتات يدوية | يضمن التقاط أي Migration مستقبلية تلقائياً دون كود مخصّص؛ متطلب المستخدم الصريح |
| 2026-05-15 | تفعيل `EnableRetryOnFailure` في `UseSqlServer` | مرونة ضد أعطال الشبكة العابرة دون كود إعادة محاولة يدوي في كل استدعاء |
| 2026-05-15 | بناء `LoadingOverlay` / `BusyButton` / `IBusyService` كجزء من Design System في Phase 2 | متطلب المستخدم: كل عملية تظهر Loading؛ توحيد التجربة عبر شاشات لاحقة |
| 2026-05-15 | بناء `ConnectionStatusBanner` + `IConnectionMonitor` لرصد انقطاع الاتصال بـ SQL Server | متطلب المستخدم: إظهار حالة الانقطاع وعدم انهيار البرنامج |
| 2026-05-15 | إضافة Splash Screen + First-Run Setup Wizard في Phase 13 | متطلب المستخدم الصريح: عمليات قاعدة البيانات والمعالج تظهر للمستخدم النهائي في المرحلة الأخيرة بعد جاهزية باقي المنظومة |
| 2026-05-15 | استخدام Implicit DataTemplates في `DataTemplates.xaml` بدلاً من ViewLocator مخصّص | حل WPF أصلي وأبسط لربط VM بـ View؛ سيُستبدل كل DataTemplate بـ View حقيقي مرحلياً |
| 2026-05-15 | جميع PageViewModels مسجَّلة Singleton لا Transient | للحفاظ على state التنقل والبحث بين الزيارات داخل الجلسة الواحدة |
| 2026-05-15 | استخدام `ResourceKeyConverter` لتمرير أيقونات Sidebar كـ string keys في NavigationDescriptor | يفصل التنقل عن WPF Resources ويسمح بتعريف القوائم في C# pure |
| 2026-05-15 | حذف MainWindow و MainViewModel القديمين بعد الانتقال لـ MainShellView | ملفات Phase 1 الانتقالية لم تعد مستخدمة؛ AI_INSTRUCTIONS يمنع الإبقاء على Dead code |
| 2026-05-15 | استخدام `AddDbContextFactory<NasaqDbContext>` بدل DbContext scoped | WPF تطبيق Desktop بدون scope-per-request؛ Factory يتيح short-lived contexts من أي خدمة Singleton (Repositories, ConnectionMonitor, DbSeeder, DatabaseInitializer) بأمان thread-safety |
| 2026-05-15 | اختيار `BCrypt.Net-Next` لتجزئة كلمات المرور | معيار صناعي، صيانة نشطة، دعم work-factor، يلبي AI_INSTRUCTIONS (Hashing دائماً) |
| 2026-05-15 | Generic `IRepository<T>` فقط في Phase 3 بدلاً من repos خاص لكل Entity | YAGNI — Acceptance criteria تتطلب فقط قراءة Students؛ Repositories متخصصة (StudentsRepository إلخ) ستُضاف عند الحاجة لاستعلامات domain-specific في Phase 6+ |
| 2026-05-15 | App.OnStartup async + استدعاء InitializeAsync عبر `Task.Run().ConfigureAwait(true)` | منع deadlock بـ WPF SynchronizationContext أثناء انتظار EF Core async migrations؛ MessageBox عربي عند الفشل ثم Shutdown — سيُستبدل بـ Splash في Phase 13 |
| 2026-05-15 | إضافة `IDesignTimeDbContextFactory<NasaqDbContext>` (`NasaqDbContextFactory.cs`) | يضمن نجاح `dotnet ef migrations add/database update` دون تشغيل WPF host (يقرأ appsettings.json مباشرة) |
| 2026-05-15 | إضافة `ActivateAsync` على `PageViewModel` كنقطة دخول لتحميل بيانات الصفحة | MainShell يستدعيها بعد كل تنقّل؛ مكان موحّد للأشياء async بدلاً من فايبر-end-fire-and-forget داخل الـ Constructor، يحافظ على Singleton ViewModel بدون state تالف |
| 2026-05-15 | حفظ DateTime UTC في DB واستخدام `datetime2` (افتراضي EF Core 8) | يلبي AI_INSTRUCTIONS؛ Phase 13 ستُضيف Hijri formatter عند العرض |
| 2026-05-15 | `ShutdownMode="OnExplicitShutdown"` + إدارة دورة حياة `LoginView`/`MainShellView` من `App` مباشرة | تبديل النوافذ أثناء Login/Logout يحتاج للحياة بعد إغلاق نافذة (لو OnLastWindowClose لانتهى التطبيق فور إغلاق Login عند نجاحه)؛ App يتعامل مع `CurrentUserService.SignedIn/Out` ليُنشئ النافذة المناسبة |
| 2026-05-15 | `PasswordBox` مربوط بـ ViewModel عبر code-behind وليس Binding | WPF لا يكشف `Password` كـ DependencyProperty لأسباب أمنية؛ التعامل اليدوي عبر `PasswordChanged` معيار رسمي ولا يحفظ النص في ذاكرة managed أطول من اللازم |
| 2026-05-15 | `MainShellViewModel` يستمع لـ `CurrentUserService.SignedIn` ويُعيد التنقل إلى Dashboard | الـ VM Singleton لذا يحتاج لإعادة تعيين حالته بين الجلسات بدلاً من إنشاء instance جديد (الذي سيتطلب Scope) |
| 2026-05-15 | فصل `Converters.xaml` كـ ResourceDictionary مستقل وإضافته في `App.xaml` قبل Common | Converters تُستخدم في عدة Themes/Views؛ فصلها يمنع التكرار ويبقي الترتيب صحيحاً (Common يعتمد عليها لاحقاً) |
| 2026-05-15 | اعتماد `LiveChartsCore.SkiaSharpView.WPF` 2.0.0-rc5.4 لرسوم Phase 5 | الـ rc5 مستقر داخل LiveCharts2 v2 ومدعوم رسمياً لـ WPF .NET 8 + ميزات Cartesian/Pie/PolarChart؛ مرشّح Agent.md لم يفرض إصداراً سابقاً |
| 2026-05-15 | تغليف `CartesianChart` و`PieChart` بـ `FlowDirection="LeftToRight"` داخل لوحة Dashboard RTL | محاور SkiaSharp تُرسم بإحداثيات شاشة فعلية؛ تركها في حاوية RTL يقلب اتجاه الزمن على المحور X ويعكس التسميات. الحل النهائي: إبقاء التطبيق كله RTL، وتحويل حاوية الرسم فقط لـ LTR. النصوص العربية فوق الرسم تُغلَّف بـ RTL StackPanel منفصل |
| 2026-05-15 | `IDashboardService` يُرجع `DashboardSnapshot` واحد بدل سلسلة Calls من الـ ViewModel | تقليل round-trips إلى DbContextFactory، استعلام واحد متماسك يضمن snapshot لحظي متّسق، يبسط معالجة الخطأ في `RefreshCommand` (try واحد) |
| 2026-05-15 | استخدام Grid عمودَين بدل DockPanel لترويسة DashboardView | في وضع RTL تتعارض دلالات `DockPanel.Dock="Right"` مع `HorizontalAlignment="Left"` فيتجمعان معاً على الجانب البصري نفسه. Grid مع `*` ثم `Auto` يضمن وضوحاً صريحاً: العمود 0 (يميناً في RTL) للعنوان، العمود 1 (يساراً) للزر — قاعدة عامة تنطبق على كل ترويسات الشاشات اللاحقة |
| 2026-05-15 | إضافة `StudentsRepository` متخصصاً يكمل `IRepository<T>` العام | Phase 6 يحتاج استعلامات Domain-specific (Search متعدد الحقول، Pagination، Stats، Lookups، Editor projection، Transactional Save) لا تُلائم API الـ Generic؛ القرار في Phase 3 كان "نضيف repos متخصصة عند الحاجة" — هذه أول حاجة فعلية. الاثنان يتعايشان |
| 2026-05-15 | تخزين صور الطلاب خارج مجلد التطبيق في `%LocalAppData%/Nasaq/Photos/Students/{guid}.ext` | تجنّب نسخ الملفات إلى `bin/` وضمان أنها تنجو بين عمليات Rebuild؛ المسار قابل للنسخ الاحتياطي لاحقاً مع DB |
| 2026-05-15 | الـ Editor يُعرض في نفس الصفحة (CurrentMode في الـ ViewModel) لا في Modal Window | يطابق التصميم 4 (نفس Sidebar/TopBar)، تجربة مستخدم أفضل من Dialogs على شاشة كبيرة، يبقي الـ ViewModels محقونة بـ DI بدون Owner Window |
| 2026-05-15 | Save لا يستخدم `[RelayCommand(CanExecute=...)]` — يفحص الـ validation داخلياً ويعرض الخطأ كـ ErrorMessage | Validation متعددة الحقول مع رسائل عربية واضحة تتطلب logic أغنى من CanExecute البسيط؛ Banner أحمر يعطي تجربة أوضح من Button معطّل بلا سبب ظاهر |
| 2026-05-15 | بحث الـ Students مع Debounce يدوي عبر `Task.Delay` + Cancellation داخل setter | تجربة مستخدم لحظية بدون hammer للقاعدة على كل ضغطة مفتاح؛ بدائل (Reactive) تضيف اعتمادية كبيرة بلا داعٍ في WPF |
| 2026-05-15 | Transactions يدوية تُغلَّف داخل `Database.CreateExecutionStrategy().ExecuteAsync(...)` | `EnableRetryOnFailure` يرفض `BeginTransactionAsync` المباشر برسالة `SqlServerRetryingExecutionStrategy does not support user-initiated transactions`. الـ Strategy يعيد العملية كاملة عند فشل عابر بدلاً من جزء منها |
| 2026-05-15 | تخزين الصور كـ `byte[]` (`varbinary(max)`) داخل عمود `Student.PhotoBytes`، **لا** على نظام الملفات | متطلب المستخدم الصريح: «جميع البيانات تحفظ في قاعدة البيانات حتى الصور، لا يحذف أي شي محلياً». يبسّط النسخ الاحتياطي (نسخة DB واحدة = كل المحتوى) ويوحّد دورة حياة البيانات. كلفة الحجم مقبولة لمدرسة واحدة |
| 2026-05-15 | إضافة `StudentSaveModel.UpdatePhoto: bool` بدلاً من تخمين النية من قيمة `PhotoBytes` | في تدفّق التعديل، إذا كان `PhotoBytes=null` غير معلوم هل المستخدم أزال الصورة أم لم يلمسها؛ الشارة الصريحة تمنع مسح الصورة عن طريق الخطأ |
| 2026-05-15 | `IErrorReporter` + `ErrorWindow` بدلاً من `MessageBox` للأخطاء التقنية | متطلب المستخدم: «نظام اقتناص أخطاء عام بنافذة خاصة + زر نسخ الخطأ كامل». Dispatcher/AppDomain/TaskScheduler الثلاثة موصولون لمنع أي انهيار صامت |
| 2026-05-15 | `IToastService` + `ToastHost` كنمط الإخطار الافتراضي للنجاح/التحذير/المعلومة | متطلب المستخدم: «إضافة Toast عند إجراء أي عملية بتصميم احترافي». MessageBox يبقى للحوارات التي تتطلب قراراً (تأكيد حذف/أرشفة) فقط |
| 2026-05-15 | إعادة تخطيط الشاشات: لا scroll على مستوى الصفحة، DataGrid يحتوي السكرول، Pagination ثابت أسفل، action bar للـ Editor ثابت أعلى، حذف بطاقات الإحصائيات لصالح سطر مدمج | متطلب المستخدم الصريح: «حذف الإحصائيات أعلى الصفحة وتكبير الشبكة، الشبكة تتضمن السكرول وليس الصفحة، نقل أزرار الإضافة إلى الجزء العلوي وتكون ثابتة، اضغط التصميم قدر المستطاع بدون تصغير الأدوات». القاعدة موثّقة في AI_INSTRUCTIONS لتطبَّق على شاشات Phase 7+ |
| 2026-05-15 | تغليف `CartesianChart` و`PieChart` بـ `FlowDirection="LeftToRight"` رغم أن الواجهة RTL | SkiaSharp يعكس المحور X والـ legends عند `RightToLeft`؛ overlay عربي RTL منفصل يعرض رسائل الـ empty-state دون مسّ بنية الرسم |
| 2026-05-15 | إخفاء تحذيرات NU1701 عبر `<NoWarn>` في csproj | تبعات SkiaSharp.Views.WPF + OpenTK تُحزَّم لـ net4x لكنها تعمل على net8.0-windows داخل WPF؛ القرار محدود لهذا المشروع للحفاظ على Build 0 Warning |
| 2026-05-15 | DashboardService يُرجع `record` snapshot واحد بدل عدة Tasks متوازية | استدعاء واحد من الـ ViewModel أبسط للـ Loading/Error handling، ويستخدم سياق DbContext واحد في كل استعلام (وفّرناها كذلك) |
| 2026-05-15 | فصل `DashboardViewModel` في ملف خاص خارج `PageViewModel.cs` | الـ VM للـ Dashboard كبر بشكل كافٍ (chart axes/series + 18 خاصية)؛ بقية PageVMs لا تزال stub داخل الملف المشترك حتى تكتمل مراحلها |
| 2026-05-15 | Empty-state لـ donut اليوم عبر شريحة رمادية واحدة بقيمة 1 | LiveCharts2 يفشل عند `Values=Array.Empty<>()`؛ الشريحة الرمادية تحفظ الشكل البصري ويظهر فوقها overlay "0٪" |
| 2026-05-15 | تفضيلات المستخدم (RememberMe + ترتيب الطلاب + حجم الصفحة) تُحفظ في `%LOCALAPPDATA%/Nasaq/prefs.json` لا في DB | هذه تفضيلات Per-Machine لا تنتمي لبيانات المدرسة القابلة للنسخ الاحتياطي؛ DB قد لا تكون متاحة عند بدء التشغيل قبل تسجيل الدخول. لا تتعارض مع قاعدة DB-only لأنها استثناء صريح موثَّق |
| 2026-05-15 | بناء `Nasag.Views.Common.NasaqDialog` كنافذة موحَّدة بديلة لكل `MessageBox.Show` | متطلب المستخدم: «Message Boxes احترافية متوافقة مع ثيم النظام». نافذة بحواف مدورة، Tajawal، أيقونة ملوّنة في الرأس، RTL، 5 kinds (Info/Success/Warning/Danger/Question) + Confirm/ConfirmDestructive من خلال `IDialogService` |
| 2026-05-15 | بناء `Nasag.Controls.SearchableComboBox` بدلاً من ComboBox الافتراضي | متطلب المستخدم: «Comboboxes احترافية قابلة للبحث، اقتراحات، اختيارية». UserControl يلفّ TextBox + Popup ListBox، فلترة Contains (case-insensitive)، Arrow/Enter/Esc، تنظيف الاختيار بزر ×. ComboBox القياسي لا يدعم البحث |
| 2026-05-15 | DataGrid Theme معدّل: `GridLinesVisibility="All"` + `HorizontalContentAlignment="Center"` لكل خلية ورأس | متطلب المستخدم الصريح: «إضافة خطوط شبكة لجميع الخلايا والأعمدة لتكون مخططة بالكامل + محاذاة الشبكة والأعمدة إلى الوسط تماماً». موحَّد عبر الـ Theme لتطبيقه على كل الجداول القادمة بدون تكرار |
| 2026-05-15 | اعتماد `ClosedXML 0.104.2` لتصدير/استيراد Excel | متطلب «تصدير Excel منظّم احترافي + معالج استيراد متكامل». ClosedXML أعلى-API من OpenXML SDK، MIT، مدعوم لـ net8.0، يُنتج .xlsx حقيقي (ليس CSV). 20 عمود عربي + freeze pane + banded rows + autofit |
| 2026-05-15 | تخزين Toast Host في `FlowDirection=LeftToRight` منفصل لضمان الموقع البصري الأيسر بغض النظر عن FlowDirection المحيط | الـ Shell كله RTL؛ HorizontalAlignment داخل RTL يُعكس. حلّ Pinning الـ Host بـ Stretch + داخلياً LTR + HorizontalAlignment.Left يثبت الموقع البصري بصورة قاطعة. هذه القاعدة موثَّقة في AI_INSTRUCTIONS.md (UI Component Standards) |
| 2026-05-15 | `BubbleButton` كزر CTA وحيد لكل شاشة قائمة (CornerRadius=999 + Teal shadow glow) | متطلب المستخدم: «تحويل زر إضافة طالب إلى شكل Bubble Button لافت». الـ Style مفصول في `Themes/Buttons.xaml`، لا يحلّ محل `PrimaryButton` (للنماذج)، استخدامه محصور بالـ CTA |
| 2026-05-15 | Pagination ComboBox قابل للكتابة (IsEditable=True) ينتقل عند Enter | متطلب المستخدم: «Combobox مخصص للـ Pagination + السماح بكتابة رقم الصفحة يدوياً». فعالية مزدوجة: قائمة لكل الصفحات + قفز مباشر بدون قائمة |
| 2026-05-15 | Import Wizard كنافذة Modal بأربع خطوات لا كصفحة | متطلب «معالج استيراد متكامل». الـ Modal يحفظ السياق الأم في الذاكرة، الحالة محصورة في الـ Wizard، Confirm Destructive يعيد التحقق قبل تنفيذ "حذف ثم استيراد" |
| 2026-05-15 | StudentEditor: الصورة في Button Template مخصّص (Full dropzone) | متطلب «إصلاح ميزة رفع الصورة + ظهور المنطقة فارغة وجاهزة». اعتبار 200x200 كاملاً سطحاً قابلاً للنقر يضمن لا توجد حالة "البرنامج لا يفعل شيئاً عند النقر"؛ Hover بحدّ Teal يوضح بصرياً أن المنطقة Clickable |
| 2026-05-15 | اختصارات لوحة المفاتيح موثَّقة في UI Standards (Ctrl+N/F5/Delete/Ctrl+F للقوائم + Ctrl+S/Esc للنماذج) | متطلب «اختصارات الحفظ والحذف على مستوى النظام». UserControl.InputBindings يحقن الـ KeyBinding مباشرة على الشاشة؛ Ctrl+F يحتاج Focus للـ TextBox لذا يُحقن في code-behind |
| 2026-05-16 | ComboBox theme: ToggleButton يغطي الحقل كاملاً + طبقة نص فوقه `IsHitTestVisible=False` | النمط الرسمي لـ WPF Aero يضمن أن النقر في أي مكان من الحقل يفتح الـ Popup بنقرة واحدة. وضع ContentPresenter داخل الـ Toggle كان يبتلع النقرات أحياناً ويتطلب نقرتين |
| 2026-05-16 | BytesToImage يستخدم `BitmapFrame.Create` بدلاً من `BitmapImage` | BitmapFrame أكثر تسامحاً مع ملفات مختلفة الأبعاد/الـ DPI، ويتيح فحص `PixelWidth>0` كاختبار قابلية العرض دون رمي استثناء، فيمكن منع رفع صور تالفة قبل قبولها |
| 2026-05-16 | حذف الطالب عبر سلسلة `ExecuteDelete` بدلاً من Tracking + SaveChanges | EF cascade FK لا يكفي لأن بعض العلاقات `Restrict` صراحةً (StudentFee→Student، Mark→Subject). Pipeline صريح بالترتيب (Payments → Installments → Fees → Attendance → Marks → Student → Guardian اليتيم) آمن ومستقل عن سلوك FK |
| 2026-05-16 | حذف الصف بـ Cascade مع تأكيد مزدوج بدلاً من المنع | متطلب المستخدم الصريح. التأكيد الأول للنية، التأكيد الثاني يعرض عدد الشعب/الطلاب/المواد الفعلي قبل التنفيذ ليقلل الأخطاء البشرية. الـ Pipeline يحذف الشعب وكل تبعياتها ثم Subjects وFeePlans والصف نفسه داخل Transaction واحدة |
| 2026-05-16 | Move-student dialog يستخدم `SearchableComboBox` مع `MoveTargetSection.Display="الصف — الشعبة (count/capacity)"` | تجربة مستخدم أقوى من Dropdown بسيط: المعلم يبحث بالنص ويرى الامتلاء قبل الاختيار. القائمة تستثني الشعبة الحالية للطالب بطبيعتها |
| 2026-05-16 | Section creation يتطلب `AcademicYear` نشطة من `SchoolSettings.CurrentAcademicYearId` ثم fallback إلى أحدث `IsActive` | المخطط: `Section.AcademicYearId` إلزامي + Unique index على (GradeId, AcademicYearId, NameAr). الـ Lookup المركَّز يضمن أن كل الشعب الجديدة تُلصق بالسنة الصحيحة دون أن نطلب من المستخدم اختيارها يدوياً |
| 2026-05-16 | StudentsViewModel يحقن `IClassesRepository` مباشرة لإجراء Move-student | تجنّب تبعية دائرية أو حقن ViewModel→ViewModel. الـ Repository واجهة domain خفيفة، والـ ClassesViewModel يستخدمها أيضاً، فلا تكرار في الكود |
| 2026-05-16 | تخفيف `BubbleButton`: من Pill (`CornerRadius=999`) + Teal glow → زر CTA مدوّر باعتدال (`RadiusMd`=10) بدون ظل | بعد التطبيق على الشاشة الفعلية ظهر أن الـ pill + الظل التوهّجي مبالغ فيهما وسيؤثران على كل الشاشات اللاحقة. القرار: الإبقاء على الاسم لتجنّب breakage في الـ docs/الكود، لكن تطوير المظهر ليتسق مع PrimaryButton مع الحفاظ على لون Teal الكامل كإشارة CTA |
| 2026-05-16 | حفظ الحضور بـ Upsert على `(StudentId, Date.Date)` مع عرض الطلاب النشطين فقط | فهرس `AttendanceRecords` يمنع التكرار على الطالب/اليوم؛ تطبيع التاريخ إلى Date-only يمنع تكرار نفس اليوم بسبب الوقت، واستبعاد غير النشطين يحافظ على سجل الشعبة اليومي الحقيقي |
| 2026-05-18 | فصل شاشتي «المواد الدراسية» و«أنواع الامتحانات» إلى عنصرين مستقلين في القائمة الجانبية (لا Tabs) | Subjects تربط بـ Grade (شبه ثابت)، Exams تربط بـ AcademicYear (يتغير سنوياً). توحيدهما في شاشة واحدة سيضيف منطق التبويب وتعقيداً لا داعي له؛ شاشتان مستقلتان تتبعان نفس نمط `ClassesView` تماماً |
| 2026-05-18 | حذف الدرجة عندما يفرّغ المستخدم الـ Value في شاشة Marks Entry (لا حفظ صفر) | الـ index الفريد على (Student, Subject, Exam) لا يميّز بين «صفر» و «غير ممتحن»؛ التمييز يكون بوجود السجل نفسه أو عدمه. تفريغ الخلية → DELETE، إدخال 0 → INSERT/UPDATE بـ Value=0. هذا يدعم منطق `weightSum==0 → IsAbsent` في الـ Calculator |
| 2026-05-18 | `IResultsCalculator` كـ Pure logic Singleton منفصل عن Repository | فصل المنطق عن الـ I/O يجعل القاعدة (متوسط مرجَّح + سياسة النجاح + التقدير) قابلة للاختبار وحدوياً وقابلة لإعادة الاستخدام في Reports/Dashboards لاحقاً بدون لمس DB |
| 2026-05-18 | سياسة النجاح في الـ Calculator: لا نجاح حتى ينجح الطالب في **كل** المواد ولا توجد مادة غير ممتحنة | متطلب الـ Phase 9: «حساب النجاح/الرسوب». الحل البديل (نجاح بمعدل ≥50% فقط) قد يخفي رسوب في مادة جوهرية. السياسة الصارمة تعكس واقع التقييم المدرسي العربي وتُظهر القوائم `FailedSubjects` و `MissingSubjects` بشكل مفيد للمعلم |
| 2026-05-18 | في حساب درجة المادة: `weightSum` يجمع أوزان الامتحانات الموجودة فقط (لا كل الامتحانات المخطَّطة) | عدم عقاب الطالب على درجة لم تُدخل بعد (مثلاً نهائي قبل موعده). الـ Calculator يميّز بين «صفر فعلي» و«غير ممتحن» — الأخير لا يدخل في الحساب لكنه يقطع شرط النجاح |
| 2026-05-18 | تنفيذ Phase 9 عبر 4 وكلاء برمجة بالتوازي بعد إعداد `IResultsCalculator` يدوياً | السرعة + استقلالية الميزات: Subjects/Exams/Marks/Results كل منها 4-5 ملفات بأنماط متكررة من Phase 7/8. كل وكيل أُعطي نمطاً مرجعياً صريحاً (`AttendanceRepository.cs`, `ClassesRepository.cs`, `AttendanceViewModel.cs`, `AttendanceView.xaml`, `SectionEditorDialog.xaml`). الدمج النهائي (DI/Navigation/DataTemplates) نُفّذ يدوياً لتجنّب سباق على الملفات المركزية |
| 2026-05-18 | إضافة `ResultGrade.Pending` + `ExaminedMax` بعد screenshots المستخدم أظهرت كل الطلاب كـ«راسب» بنسبة 9-19% | السياسة الأصلية «النجاح يتطلب إكمال كل المواد» كانت صحيحة منطقياً، لكنّ صياغة «راسب» قبل اكتمال الفصل مضلِّلة بصرياً. الحل: حالة ثالثة Pending تُحفظ النية (الطالب لم يُختبر بعد، ليس راسباً)، والنسبة تُحسب على ExaminedMax (مجموع MaxMark للمواد المُمتحَنة فقط) بدلاً من MaxTotal الكلي. الـ Failed يُحجَز للطلاب الذين رسبوا فعلاً في مادة. الـ MaxTotal يبقى في الـ DTO كـ tooltip للسياق |
| 2026-05-18 | دورة مراجعة-إصلاح-تدقيق بـ 10 وكلاء (4 تنفيذ + 3 مراجعة + 3 إصلاح + 1 تدقيق نهائي) | بعد screenshots المستخدم، الأنماط المتكررة تبيّن أن Phase 9 يحتوي مشاكل UX/Logic لا تُكتشف بـ Build فقط. المراجعة التعاونية (UX/Logic/Quality) كشفت 35+ ملاحظة موزَّعة بطبقات مختلفة. الإصلاح متوازياً ضمن حدود ملفات متعارضة، ثم تدقيق نهائي مستقل لتجنّب bias. القاعدة الجديدة: لكل Phase بصرية، اطلب screenshots من المستخدم بعد التنفيذ المبدئي وقبل اعتباره مكتملاً |
| 2026-05-19 | شاشة الرسوم بنمط Single-Student (لا قائمة طلاب) | يطابق التصميم 9 المرجعي. الرسوم كيان فردي بطبيعته (لكل طالب خطة وأقساط ومدفوعات مستقلة)، وعرض قائمة + شاشة تفاصيل يضاعف العمل دون قيمة. التدفّق Grade→Section→Student يستفيد من SearchableComboBox الموجود ويُبقي السياق واضحاً لمشرف القبول |
| 2026-05-19 | توليد رقم السند `REC-yyyyMMdd-nnnn` ذرّياً داخل نفس الـ Transaction بدل `Helper` خارجي | الـ Helper يُنشئ DbContext منفصلاً ويفقد الذرّية مع الـ Payment INSERT. الـ Counter داخل الـ tx يستعلم من نفس الـ ctx ثم يُدخل السجل قبل Commit. مخاطر التضارب على مدرسة واحدة منخفضة جداً؛ Unique index على ReceiptNumber يُغطّي الحالة النادرة بإلقاء استثناء يُحوَّل لـ Toast Warning |
| 2026-05-19 | Overdue يُحسب لحظياً عند القراءة فقط، Status في DB يبقى كما هو | تحديث Status تلقائياً في DB يتطلّب Job دوري أو تحديث-عند-القراءة (write-on-read anti-pattern). الـ Calculator في الـ DTO يكفي للعرض، ويوفّر مرونة لاحقة لإلغاء الـ Overdue إن تأخر الدفع بسبب موافقات إدارية مثلاً |
| 2026-05-19 | RecordPayment يرفض الدفع الزائد على مستويين (إجمالي الرسوم + القسط المحدد) | لا overpayment على الطالب الواحد؛ لا overpayment على قسط بعينه عند ربط الدفعة به. الرسالة بالعربية تظهر كـ Toast Warning. هذا يبقي PaidAmount متّسقاً مع TotalAmount دون الحاجة لـ Check Constraint في DB |
| 2026-05-19 | تنفيذ Phase 10 بوكيلين بالتوازي (Data + UI) مع عقد DTO صريح في كلا الـ briefs | السرعة + استقلالية الطبقات. كلا الوكيلين يُشار له بالنوع بـ "name only" دون قراءة عمل الآخر، والـ build النهائي يربطهما. الـ audit نُفّذ كوكيل ثالث مستقل بعد البناء؛ النتيجة Ready بـ 0 must-fix |
| 2026-05-19 | طباعة سند القبض وكشف الحساب عبر WPF FlowDocument + PrintDialog بدلاً من QuestPDF | تجنّب إضافة NuGet في Phase 10.1؛ FlowDocument يدعم Tajawal + RTL أصلياً ويفي بمتطلبات A4 ومعاينة الطباعة. QuestPDF تُؤجَّل لـ Phase 11 (تقارير عبر التطبيق كله) للاستفادة من واجهة موحَّدة آنذاك. هذا يُسرّع تسليم Phase 10 ولا يفرض إعادة عمل لاحقاً (الـ models والـ FlowDocument helpers سيحلّ محلّهما QuestPDF builders بتغيير سطح API صغير) |
| 2026-05-19 | إخفاء أزرار الكتابة عبر `Visibility` وليس فقط `IsEnabled` للمستخدمين بلا `Permission.ManageFees` | تعطيل الأزرار يترك إشارة بصرية مربكة؛ الإخفاء يجعل التجربة نظيفة للمعلم/مدير الشعبة الذي لا يملك صلاحية المحاسبة. الـ Bubble CTA «تسجيل دفعة» + زر «+ دفعة» في صف القسط + زر الحذف في صف الدفعة. الأزرار للقراءة (Refresh/Print/Statement) تبقى ظاهرة للجميع |
| 2026-05-19 | عبور Students → Fees عبر `PrepareForStudentAsync` على FeesViewModel (Singleton) بدلاً من تمرير بارامتر عبر NavigationService | الـ VM Singleton يحفظ السياق بين الشاشات، والاستدعاء المباشر يتجنّب توسيع NavigationService بـ generic payload. تدفّق: `NavigateTo(Fees)` ثم `_services.GetRequiredService<FeesViewModel>().PrepareForStudentAsync(studentId)`. مطابق لنمط Move-student في Phase 7 |
| 2026-05-19 | عند توليد أقساط بخطة مُعيَّنة، باقي التقريب يُدحرج على القسط الأخير | يضمن Σ(Installments) == StudentFee.TotalAmount بالضبط، يمنع انحراف تراكمي يربك المستخدم عند جمع الأقساط يدوياً. تكلفة: قسط أخير قد يختلف بـ 0.01-0.0N ر.س عن الأقساط الأخرى، مقبول بصرياً |
| 2026-05-21 | فرض صلاحية `Permission.ManageFees` داخل `FeesRepository` (لا في الـ VM فقط) عبر `ICurrentUserService.HasPermission(...)` | الـ VM-only check قابل للتجاوز برمجياً (أي service آخر يستدعي الـ Repo مباشرة). الـ Repo-level check يجعل الـ permission ثابتة بغض النظر عن نقطة الدخول. helper `HasPermission` على الـ service يلغي تكرار `_currentUser.User?.Role?.Permissions.HasFlag(...)` في كل VM |
| 2026-05-21 | استخدام `ExecuteUpdateAsync` على `StudentFee.PaidAmount` و `Installment.PaidAmount` بدل قراءة-تعديل-حفظ في الذاكرة | يحمي من dirty-write تحت تزامن دفعات متعددة على نفس الطالب/القسط، ويُلغي الحاجة لـ rowversion. التعبير الشرطي `< 0m ? 0m : (X - amount)` يحافظ على حد الصفر ذرّياً. التغيير لا يكسر منطق `EnableRetryOnFailure` لأن العملية ضمن نفس الـ ExecutionStrategy |
| 2026-05-21 | retry حلقي ×3 على `DbUpdateException` لـ UNIQUE violation رقم السند بدل قفل توزيع | الـ Counter داخل الـ tx يستعلم `COUNT(*)+1` فيُمكن أن يولّد رقماً مكرراً تحت تزامن. الـ retry مع توليد رقم جديد بعد كل فشل أبسط وأرخص من قفل serializable أو جدول counter. الحلقة خارج `strategy.ExecuteAsync` لأن BeginTransaction لا يجوز داخل catch |
| 2026-05-21 | فلترة `NavigationItems` بحسب `Permission` المطلوبة لكل قسم (Sidebar gating) | حتى لا يرى المعلم/المحاسب عناصر قائمة لا يستطيع الوصول إليها — تجربة أنظف وأقل ارتباكاً من شاشات مقيَّدة. إعادة بناء القائمة عند SignedIn/SignedOut + إعادة توجيه لـ Dashboard إذا كان المستخدم داخل قسم فقد صلاحيته (تغيير دور أو dev override) |
| 2026-05-21 | `MoneyFormatter` مركزي بثقافة `ar-SA` لكل سلاسل العملة | استبدال " ر.س" hard-coded في 7+ مواقع. لو احتاج عميل تغيير العملة لاحقاً، النقطة الواحدة تكفي. الـ Format يستخدم `ar-SA` ليُنتج فواصل الآلاف العربية ومنزلتين عشريتين موحَّدتين عبر التطبيق |
| 2026-05-21 | RESTORE لا يكتب صف audit في `BackupLogs` | `RESTORE … WITH REPLACE` يستبدل القاعدة بمحتوى الـ .bak فيمحو أي صف أُضيف قبله (سواء داخل نفس الاتصال أو من اتصال آخر). كتابة صف Restore تكون مضلِّلة لأنها تختفي بمجرد نجاح العملية. الـ Backup row الذي أنتج الـ .bak يبقى مرجع الحقيقة، وبعد نجاح RESTORE يُغلق التطبيق فوراً (Application.Shutdown) لإعادة تشغيل نظيف. إن لزم تدقيق Restore لاحقاً، نضيفه في ملف log منفصل في `%LOCALAPPDATA%\Nasaq\Backups\restore.log` (مؤجَّل لـ Phase 13) |
| 2026-05-21 | BACKUP/RESTORE عبر `Microsoft.Data.SqlClient` مباشرة بدل EF Core | `Database.MigrateAsync`/`ExecuteSqlRawAsync` يلفّ DDL في transaction، وBACKUP DATABASE لا تُسمح داخل transaction. لذا نفتح `SqlConnection` خام بـ CommandTimeout=0. مسار الملف يمر عبر `SqlParameter @path` (SqlClient يدعمه عبر sp_executesql wrapping). اسم القاعدة من `SqlConnectionStringBuilder.InitialCatalog` ويُحقن في bracket-quoted identifier بعد تحقق whitelist (`IsSafeDatabaseName`: letters/digits/_/-/space ≤128 char) |
| 2026-05-21 | RESTORE يستخدم اتصال master منفصل بـ `Pooling=false` + `MultipleActiveResultSets=false` | الاتصال المجمَّع قد يعود بحالة قديمة تشير للقاعدة بعد `SET SINGLE_USER`. اتصال master single-shot يضمن أن `RESTORE WITH REPLACE` ينفّذ على master وأن الاتصال يُلقى بعد العملية. `try/finally` على `SET MULTI_USER` يحمي من قفل دائم في حالة فشل RESTORE |
| 2026-05-21 | بعد نجاح RESTORE، إغلاق التطبيق بدلاً من sign-out + إعادة دخول | حالة EF Core DbContext + identity columns + جلسة المستخدم الحالي تشير لقاعدة لم تعد موجودة فعلياً. أعمدة Identity قد تكون مختلفة، rowversion مختلفة، البيانات كلها استُبدلت. إعادة تشغيل العملية تماماً أبسط وأأمن من محاولة re-hydrate كل state in-process. UX: نافذة Success تخبر المستخدم + Shutdown(0) |
| 2026-05-21 | `Permission.ManageUsers` هو تعريف "مدير" لحماية آخر admin | تجنّب hard-coded "admin" username أو IsSystem flag (قد تتغير في النشر). أي دور لديه ManageUsers يُعتبر admin. `UsersRepository.SetActiveAsync/DeleteAsync/UpdateAsync/UpdateRolePermissionsAsync` يعدّ المسؤولين النشطين الباقين قبل أي تغيير مدمّر. يحمي من فقدان الوصول بسبب delete/deactivate/role-strip |
| 2026-05-21 | `LogoBytes varbinary(max)` على `SchoolSettings` + الإبقاء على `LogoPath` legacy | يتبع نمط `Student.PhotoBytes` (الصور في DB، لا قرص محلي — متماشٍ مع Data Storage Rule). `LogoPath` يبقى للـ backward-compat (موجود في migration `InitialCreate` ولا يُكتب من Phase 12)، حذفه يتطلب migration drop + قد يكسر قواعد بيانات قائمة. سيُزال في Phase 13 عند تدقيق Schema |
| 2026-05-21 | Users screen بدون pagination/page-size | عدد المستخدمين في مدرسة نموذجية ≤ 50 (Admin + مدير + معلمون + محاسب). البحث + فلتر الدور + ActiveOnly يغطّيان الـ UX. إضافة pagination كاملة (Page/Skip/Take + Footer + PageNumbers + JumpTo) ستكون code-bloat مقابل قيمة قليلة. يُعاد النظر إن وجدت حالة فعلية بمستخدمين > 100 |
| 2026-05-21 | تأجيل آلية backup تلقائي مجدول لـ Phase 13 | Phase 12 يوفّر backup يدوي + استعادة + سجل. backup مجدول (مرة أسبوعياً مثلاً) يحتاج background worker + إعدادات جدولة + تنبيهات. أُجّل لـ Phase 13 ضمن polish النهائي لتجنّب توسعة scope الـ Phase الحالية |
| 2026-05-21 | Splash يطبّق Migrations + Seed مرة لكل تشغيل عبر `IDatabaseInitializer.InitializeAsync` بدلاً من تقسيم progress-callback لكل خطوة | الـ initializer monolithic بحكم Phase 3، تقسيمه يتطلب refactor لـ `IProgress<DatabaseInitProgress>` مع ripple على signature. Splash يحدّث `StatusMessage` optimistically قبل/أثناء/بعد الاستدعاء، وهو كافٍ بصرياً (المستخدم يرى رسائل عربية متسلسلة). إن لزم progress حقيقي مستقبلاً، يضاف callback parameter في الـ initializer دون كسر API |
| 2026-05-21 | `ConnectionStringProvider.Current` يضمن قيمة غير فارغة دائماً (LocalDB default fallback) | بدلاً من boot-time check يمنع بناء الـ Host عند غياب أي connection string، نضمن صلاحية بناء الـ Host دائماً وندع الـ splash→CannotConnect→Wizard flow الطبيعي يتولّى. يبسّط App.OnStartup ويوفّر للمستخدم مسار recovery واحد فقط (المعالج) بدلاً من dialog مغلق |
| 2026-05-21 | Restart للتطبيق بعد حفظ Wizard بدلاً من runtime swap لـ DbContextFactory | `IDbContextFactory<T>` يخزّن DbContextOptions immutable بعد الـ Build؛ تبديلها in-process يتطلب إعادة بناء Host كامل (يكسر state الـ Singletons وكل الـ Repositories الـ subscribed). Process.Start + Shutdown(0) أنظف وأكثر موثوقية + يضمن طبقات إعادة retry وseed نظيفة على القاعدة الجديدة |
| 2026-05-21 | حفظ connection.json في `%LOCALAPPDATA%\Nasaq\` (per-machine، لا في DB) | per-machine بحكم طبيعته (قاعدة بيانات شبكية مختلفة لكل جهاز عميل)، ولا يُغطَّى بالـ backup/restore (وهو سلوك مرغوب: استعادة .bak على جهاز جديد لا يجب أن تستهدف خادم القديم). نفس مبدأ `prefs.json` و `BackupFolder` |
| 2026-05-21 | Settings entry → wizard لا يُجبر restart تلقائي، فقط يعرض NasaqDialog يطلب من المستخدم إعادة التشغيل | إعادة تشغيل forced أثناء جلسة عمل قد تفقد عمل غير محفوظ في شاشة أخرى (نموذج طالب، إدخال درجات…). تركها للمستخدم أكثر أماناً. على startup فقط (CannotConnect) نقوم بالـ restart لأنّ Shell لم يُفتح بعد |
| 2026-05-21 | Polish — promote `#33000000` إلى `OverlayDarkBrush` بدلاً من إعادة استخدام `OverlayBrush` (أبيض @0.67) | `OverlayBrush` semantic = "scrim فوق محتوى لإبراز loading state" (overlay فاتح)؛ `OverlayDarkBrush` semantic = "scrim مظلم خلف modal لتركيز الانتباه على dialog" (overlay داكن). دلالتان مختلفتان تستحقان مفاتيح مختلفة |
| 2026-05-21 | استبدال `IConnectionStringProvider` بـ `IConnectionRegistry` لدعم اتصالات متعددة محفوظة | المتطلب الجديد: شاشة Login تعرض القاعدة الحالية + dropdown للقواعد المحفوظة. Single-connection provider لا يدعم هذا. الـ Registry يحفظ List<SavedConnection> + ActiveConnectionId مع DisplayName عربي لكل قاعدة، Migration صامتة من الملف القديم تضمن الاستمرارية لمستخدمي Phase 13 المبكر |
| 2026-05-21 | `ConnectionRegistry.All` يعيد snapshot مُخبَّأ (مرجع ثابت بين القراءات حتى المخالفة التالية) | إذا أعدنا `.ToList()` جديداً في كل getter، الـ WPF bindings (خاصة SearchableComboBox) قد تعيد ربط القائمة أثناء تفاعل المستخدم وتفقد التحديد الحالي/التركيز. الـ snapshot يُحدَّث فقط داخل الـ lock بعد كل Add/Update/Remove ثم يُنشَر للقراء، فيستقر للـ binding |
| 2026-05-21 | كشف الخوادم عبر `Microsoft.Data.Sql.SqlDataSourceEnumerator` + 6s timeout + 3 fallback entries ثابتة | الـ enumerator يستخدم UDP broadcast على المنفذ 1434، قد يستغرق 5-10 ثوانٍ على شبكات بطيئة وقد يفشل تماماً على شبكات مقيّدة. Timeout 6s يحدّ بصرياً + fallback entries (LocalDB / . / .\SQLEXPRESS) تضمن أن المعالج لا يكون فارغاً أبداً حتى لو فشل الـ discovery كلياً. لا يرمي exception أبداً — يعيد القائمة المتوفرة |
| 2026-05-21 | `IApplicationRestarter` كخدمة DI Singleton بدلاً من method خاص في App | اللوجِك يُستخدم من 3 نقاط (App.ShowSetupWizard بعد المعالج، LoginViewModel عند SwitchConnection و AddConnection، SettingsViewModel عند Restart Confirm). الخدمة توحّد المنطق + قابلة للاختبار + لا تتطلب exposure لـ Application internals من الـ ViewModels |
| 2026-05-21 | تبديل قاعدة البيانات يتطلب إعادة تشغيل التطبيق دائماً (لا runtime swap) | `IDbContextFactory<T>` يخزّن DbContextOptions immutable بعد Host.Build. تبديلها in-process يتطلب إعادة بناء Host كامل (يفقد كل الـ Singletons + state + subscriptions). Restart نظيف + يضمن seed/migration على القاعدة الجديدة + يلغي أي in-flight queries مفتوحة. تكلفة UX مقبولة (نقرة Confirm + restart 2-3 ثوانٍ) مقابل بساطة وموثوقية الحل |
| 2026-05-21 | شاشة Login تستخدم `SearchableComboBox` للقواعد بدلاً من ListBox/Menu | متماشٍ مع UI Component Standards (القسم 14 في AI_INSTRUCTIONS): «استخدم `Nasag.Controls.SearchableComboBox` فقط لأي قائمة منسدلة». مدارس قد يكون لديها 5-10 قواعد محفوظة (مثلاً واحدة لكل سنة دراسية)، الـ Searchable يساعد على إيجاد الصحيحة بسرعة |
| 2026-05-21 | قاعدة دائمة: عند استخدام `[RelayCommand]` على دوال async، **لا تضف** لاحقة `Async` على اسم الـ Binding في XAML | مولّد CommunityToolkit.Mvvm يحذف لاحقة `Async` عند توليد اسم الـ ICommand property (`FooAsync()` → `FooCommand` لا `FooAsyncCommand`). كتابة `*AsyncCommand` في XAML تنتج binding error صامت ونقرات لا تستجيب. حدث هذا في معالج Phase 13 وأفقده الوظيفية كلياً. بقية المشروع (Reload/Save/...) يلتزم هذه القاعدة سلفاً |
| 2026-05-21 | إلغاء معالج الإعداد على المسار الأولي لا يُغلق التطبيق صامتاً | بدلاً من `Shutdown(0)` فوراً، نعرض NasaqDialog Confirm مع خيار «العودة للمعالج». يحمي المستخدم من نقرة خاطئة على × أو إلغاء بعد دخول الخطوة 4، ويعطي مساراً واضحاً للاستئناف بدلاً من إغلاق غامض |
| 2026-05-21 | Splash لا يُسجِّل LocalDB sentinel كـ SavedConnection تلقائياً | إذا فُتح التطبيق على جهاز فيه LocalDB لكن بلا appsettings ولا override، فإنّ `ActiveConnectionString` يقع على الـ default sentinel و InitializeAsync ينجح. تسجيل هذه السلسلة في `connections.json` يُخفي معالج الإعداد عن المستخدم في كل التشغيلات اللاحقة. الإصلاح: شرط `Source != "Default"` قبل الـ auto-add يضمن أنّ التسجيل التلقائي يقتصر على الحالات التي يأتي فيها الاتصال من `appsettings.json` (مسار Migration من Phase 13 السابق) |
| 2026-05-21 | قاعدة البيانات المنشأة من المعالج تكون فارغة (Roles + admin + School placeholder فقط)، لا بيانات تجريبية | الـ user الفعلي عند تثبيت البرنامج لأول مرة لا يريد 30 طالب تجريبي + خطط رسوم تجريبية يحتاج لحذفها قبل الاستخدام الحقيقي. السيدر يميّز بين dev path (full demo) و wizard path (minimal seed) عبر وجود `pending-admin.dat`. الـ School placeholder بـ `NameAr="مدرستي"` يضمن عدم crash للشاشات التي تفترض وجود سجل SchoolSettings (Reports، Fees PDF header، إلخ) — المستخدم يُحدّث البيانات الفعلية عبر شاشة الإعدادات |
| 2026-05-21 | بيانات مدير المعالج تنتقل بين الجلستين عبر ملف DPAPI-encrypted | البديل (تمريرها in-process) مستحيل لأنّ الـ wizard في الجلسة الأولى والـ seeder في الجلسة الثانية بعد restart. حفظها في `connections.json` كنص واضح خطأ أمني. DPAPI scope CurrentUser يضمن أنّ مستخدم Windows آخر على نفس الجهاز لا يستطيع قراءتها، والملف يُحذف ذرّياً عند الاستهلاك (مهما حدث) فالنافذة الزمنية للوجوده ≈ 5 ثوانٍ بين Wizard Finish و Seeder ReadAndClear |
| 2026-05-21 | السيدر يُطبّق idempotency guard (`Users.Any`) **قبل** `ReadAndClear` | إن كان السجل يحوي مستخدمين سلفاً (DB قائمة) لكن وُجد `pending-admin.dat` orphan (مثلاً user run wizard ثم cancel restart)، استهلاك الـ payload سيُهدر بيانات المدير دون استخدامها. الـ guard أولاً يضمن: orphan files تبقى آمنة، وتُستهلك فقط عند DB فارغة فعلاً |
| 2026-05-21 | `FinishAsync` ينظّف `pending-admin.dat` إن فشل registry.Add بعد Save ناجح | بدون التنظيف، الـ orphan قد يُستهلك على قاعدة بيانات لاحقة لا علاقة لها فيُنشئ admin غير متوقع. التنظيف عبر `ReadAndClear` نفسه (يُهمل النتيجة، الـ side effect هو الحذف) |

---

## 10. Issues and Risks

| المشكلة المحتملة | التأثير | التخفيف |
|----------------|---------|---------|
| دعم RTL في DataGrid افتراضي ضعيف | عرض الأعمدة معكوس | تخصيص Template كامل للـ DataGrid في `Themes/DataGrid.xaml` |
| توفر خط Tajawal على بعض الأنظمة | الواجهة قد تستخدم fallback | تضمين الخط داخل المشروع (Build Action: Resource) |
| طباعة PDF عربي ذو شكل صحيح | تقارير مشوهة | استخدام QuestPDF أو iText7 مع تفعيل ArabicShaping |
| أداء DataGrid مع آلاف الطلاب | بطء التمرير | Pagination + Virtualizing في الـ ItemsControl |
| Migrations مع EF Core على LocalDB | تعارضات schema | إدارة منظمة عبر EF Tools وعدم التعديل اليدوي |
| النسخ الاحتياطي يحتاج صلاحيات على SQL Server | فشل BACKUP DATABASE | استخدام T-SQL مباشرة + رسالة خطأ واضحة عند نقص الصلاحية |
| فشل تطبيق Migration على قاعدة موجودة (تعارض schema) | البرنامج لا يقلع | عرض الخطأ في Splash + خيار فتح Setup Wizard للترميم/إعادة الإعداد + توثيق طريقة Rollback عبر `dotnet ef database update <previous>` |
| Connection String حسّاس داخل `appsettings.json` | تسرّب كلمة مرور SQL | في Setup Wizard: حفظ Connection String في ملف إعداد مستخدم محمي (DPAPI / ProtectedData) بدلاً من JSON نصي عند استخدام SQL Authentication |
| عملية async تستمر بعد إغلاق النافذة → استثناء | crash عند الخروج | استخدام `CancellationToken` مربوط بدورة حياة الـ ViewModel/Window |
| `LoadingOverlay` يحجب التفاعل بشكل دائم بسبب استثناء غير ملتقط | تجمد UI | تغليف كل عملية async بـ try/finally يضمن `IsBusy = false` حتى عند الفشل |

---

## 11. Next Agent Instructions

أي وكيل AI يدخل المشروع بعد الآن **يجب** أن يلتزم بما يلي:

1. **اقرأ `AI_INSTRUCTIONS.md` أولاً.**
2. **اقرأ `Agent.md` بالكامل** (هذا الملف).
3. حدّد آخر مرحلة مكتملة في القسم 8.
4. أكمل من المرحلة التالية فقط — لا تعد تنفيذ مراحل مكتملة إلا إذا كانت مكسورة فعلياً.
5. لا تغير القرارات المسجلة في القسم 9 دون سبب موثّق.
6. حدّث القسم 8 و9 بعد كل عمل.
7. عند الانتهاء من جلسة، اذكر: ما تم، حالة Build، الملفات المهمة، المرحلة التالية.

**الحالة الحالية:** Phase 0–13 اكتملت — البرنامج جاهز للنشر النهائي. **توسعة Phase 13 (نفس الجلسة):** Multi-Connection Registry + Server Auto-Discovery + DB Listing + Login Picker + Application Restarter service — ملخص في إدخال Phase 13 أدناه. آخر مرحلة منجزة: **Phase 13 — Polish + Splash + Setup Wizard + Multi-Connection** (SplashWindow احترافي يدير `IDatabaseInitializer.InitializeAsync` مع 6 رسائل حالة عربية متسلسلة + معالجة مختلفة لكل failure type، First-Run Setup Wizard بـ 5 خطوات مع validation gates متعددة الطبقات + ConnectionTester عبر SqlConnection خام مع IsSafeDatabaseName whitelist + IConnectionStringProvider يكتب `%LOCALAPPDATA%\Nasaq\connection.json` ولا يرجّع قيمة فارغة أبداً، App.xaml.cs orchestration يفصل DB init من OnStartup إلى Splash flow ويحوّل Login↔Shell كما هو، Settings entry يفتح المعالج بدون restart forced، Polish: ErrorReporter RTL fallback + OverlayDarkBrush + 5 شاشات تستخدم theme brushes بدلاً من hex، README.md عربي 165 سطر/12 قسم للمستخدم النهائي). 4 Major من التدقيق المستقل أُصلحت كلها. Build 0/0. **لا توجد مرحلة Phase 14 — البرنامج مكتمل.**
