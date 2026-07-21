// Presenter HTML 转义审计（三版本共享结论）：
// ReadingViewPresenter / ReadingHtmlFormatter / FollowUpReadingPresenter
// 均通过 WebUtility.HtmlEncode（H()）写入 DOM。新增 Presenter 必须走同一路径。
// 用户输入另由 PromptInputSanitizer 剥离 ChatML 控制标记。

namespace IChing.Lab.Presentation;

internal static class HtmlEscapeAuditNote;
