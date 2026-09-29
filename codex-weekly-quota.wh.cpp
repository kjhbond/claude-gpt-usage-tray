// ==WindhawkMod==
// @id codex-weekly-quota
// @name Codex and Claude weekly quota
// @description Native XAML weekly quota beside the input indicator
// @version 1.1.0
// @author Local
// @include explorer.exe
// @architecture x86-64
// @compilerOptions -lole32 -loleaut32 -lruntimeobject -lshell32
// ==/WindhawkMod==
// GPL-3.0: taskbar XAML access helpers adapted from m417z's
// taskbar-tray-system-icon-tweaks (ramensoftware/windhawk-mods).
#include <windhawk_utils.h>
#undef GetCurrentTime
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.UI.Xaml.h>
#include <winrt/Windows.UI.Xaml.Controls.h>
#include <winrt/Windows.UI.Xaml.Controls.Primitives.h>
#include <winrt/Windows.UI.Xaml.Media.h>
#include <winrt/Windows.UI.Xaml.Media.Imaging.h>
#include <winrt/Windows.UI.Xaml.Automation.h>
#include <winrt/Windows.UI.Xaml.Documents.h>
#include <fstream>
#include <filesystem>
#include <winrt/Windows.Storage.Streams.h>
#include <ctime>
#include <shellapi.h>
#include <winrt/Windows.UI.Xaml.Input.h>
#include <winrt/Windows.UI.h>
#include <string>
using namespace winrt::Windows::UI::Xaml;
using namespace winrt::Windows::Foundation;
std::wstring WidgetPath(const wchar_t* filename) {
 wchar_t profile[MAX_PATH];
 auto length=GetEnvironmentVariableW(L"USERPROFILE",profile,ARRAYSIZE(profile));
 if(!length || length>=ARRAYSIZE(profile))return L"";
 return std::wstring(profile)+L"\\CodexQuotaWidget\\"+filename;
}
HWND g_taskbar;
UINT_PTR g_timer;
void* CTaskBand_ITaskListWndSite_vftable;

using CTaskBand_GetTaskbarHost_t = void*(WINAPI*)(void* pThis, void** result);
CTaskBand_GetTaskbarHost_t CTaskBand_GetTaskbarHost_Original;

void* TaskbarHost_FrameHeight_Original;

using std__Ref_count_base__Decref_t = void(WINAPI*)(void* pThis);
std__Ref_count_base__Decref_t std__Ref_count_base__Decref_Original;

XamlRoot GetTaskbarXamlRoot(HWND hTaskbarWnd) {
    HWND hTaskSwWnd = (HWND)GetProp(hTaskbarWnd, L"TaskbandHWND");
    if (!hTaskSwWnd) {
        return nullptr;
    }

    void* taskBand = (void*)GetWindowLongPtr(hTaskSwWnd, 0);
    void* taskBandForTaskListWndSite = taskBand;
    for (int i = 0; *(void**)taskBandForTaskListWndSite !=
                    CTaskBand_ITaskListWndSite_vftable;
         i++) {
        if (i == 20) {
            return nullptr;
        }

        taskBandForTaskListWndSite = (void**)taskBandForTaskListWndSite + 1;
    }

    void* taskbarHostSharedPtr[2]{};
    CTaskBand_GetTaskbarHost_Original(taskBandForTaskListWndSite,
                                      taskbarHostSharedPtr);
    if (!taskbarHostSharedPtr[0] && !taskbarHostSharedPtr[1]) {
        return nullptr;
    }

    size_t taskbarElementIUnknownOffset = 0x48;

#if defined(_M_X64)
    {
        // 48:83EC 28 | sub rsp,28
        // 48:83C1 48 | add rcx,48
        const BYTE* b = (const BYTE*)TaskbarHost_FrameHeight_Original;
        if (b[0] == 0x48 && b[1] == 0x83 && b[2] == 0xEC && b[4] == 0x48 &&
            b[5] == 0x83 && b[6] == 0xC1 && b[7] <= 0x7F) {
            taskbarElementIUnknownOffset = b[7];
        } else {
            Wh_Log(L"Unsupported TaskbarHost::FrameHeight");
        }
    }
#elif defined(_M_ARM64)
    // Just use the default offset which will hopefully work in most cases.
#else
#error "Unsupported architecture"
#endif

    auto* taskbarElementIUnknown =
        *(::IUnknown**)((BYTE*)taskbarHostSharedPtr[0] +
                      taskbarElementIUnknownOffset);

    FrameworkElement taskbarElement = nullptr;
    taskbarElementIUnknown->QueryInterface(winrt::guid_of<FrameworkElement>(),
                                           winrt::put_abi(taskbarElement));

    auto result = taskbarElement ? taskbarElement.XamlRoot() : nullptr;

    std__Ref_count_base__Decref_Original(taskbarHostSharedPtr[1]);

    return result;
}

using RunFromWindowThreadProc_t = void(WINAPI*)(void* parameter);

bool RunFromWindowThread(HWND hWnd,
                         RunFromWindowThreadProc_t proc,
                         void* procParam) {
    static const UINT runFromWindowThreadRegisteredMsg =
        RegisterWindowMessage(L"Windhawk_RunFromWindowThread_" WH_MOD_ID);

    struct RUN_FROM_WINDOW_THREAD_PARAM {
        RunFromWindowThreadProc_t proc;
        void* procParam;
    };

    DWORD dwThreadId = GetWindowThreadProcessId(hWnd, nullptr);
    if (dwThreadId == 0) {
        return false;
    }

    if (dwThreadId == GetCurrentThreadId()) {
        proc(procParam);
        return true;
    }

    HHOOK hook = SetWindowsHookEx(
        WH_CALLWNDPROC,
        [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT {
            if (nCode == HC_ACTION) {
                const CWPSTRUCT* cwp = (const CWPSTRUCT*)lParam;
                if (cwp->message == runFromWindowThreadRegisteredMsg) {
                    RUN_FROM_WINDOW_THREAD_PARAM* param =
                        (RUN_FROM_WINDOW_THREAD_PARAM*)cwp->lParam;
                    param->proc(param->procParam);
                }
            }

            return CallNextHookEx(nullptr, nCode, wParam, lParam);
        },
        nullptr, dwThreadId);
    if (!hook) {
        return false;
    }

    RUN_FROM_WINDOW_THREAD_PARAM param;
    param.proc = proc;
    param.procParam = procParam;
    SendMessage(hWnd, runFromWindowThreadRegisteredMsg, 0, (LPARAM)&param);

    UnhookWindowsHookEx(hook);

    return true;
}

bool HookTaskbarDllSymbols() {
    HMODULE module =
        LoadLibraryEx(L"taskbar.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!module) {
        Wh_Log(L"Failed to load taskbar.dll");
        return false;
    }

    WindhawkUtils::SYMBOL_HOOK taskbarDllHooks[] = {
        {
            {LR"(const CTaskBand::`vftable'{for `ITaskListWndSite'})"},
            &CTaskBand_ITaskListWndSite_vftable,
        },
        {
            {LR"(public: virtual class std::shared_ptr<class TaskbarHost> __cdecl CTaskBand::GetTaskbarHost(void)const )"},
            &CTaskBand_GetTaskbarHost_Original,
        },
        {
            {LR"(public: int __cdecl TaskbarHost::FrameHeight(void)const )"},
            &TaskbarHost_FrameHeight_Original,
        },
        {
            {LR"(public: void __cdecl std::_Ref_count_base::_Decref(void))"},
            &std__Ref_count_base__Decref_Original,
        },
    };

    return HookSymbols(module, taskbarDllHooks, ARRAYSIZE(taskbarDllHooks));
}



Controls::Grid g_grid{nullptr};
Controls::StackPanel g_widget{nullptr};
Controls::TextBlock g_text{nullptr};
Controls::Image g_logo{nullptr};
Controls::TextBlock g_claudeText{nullptr};
Controls::Image g_claudeLogo{nullptr};
Controls::Button g_codexButton{nullptr},g_claudeButton{nullptr};
std::wstring g_codexTip,g_claudeTip;
int g_column=-1;
Controls::MenuFlyout g_codexMenu{nullptr},g_claudeMenu{nullptr};

void OpenWebChat(bool claude) {
 ShellExecuteW(nullptr,L"open",claude?L"https://claude.ai/new":L"https://chatgpt.com/",nullptr,nullptr,SW_SHOWNORMAL);
}
void OpenDisplaySettings() {
 auto path=WidgetPath(L"QuotaSettings.exe");
 ShellExecuteW(nullptr,L"open",path.c_str(),nullptr,nullptr,SW_SHOWNORMAL);
}
void OpenAccountHelp() {
 ShellExecuteW(nullptr,L"open",L"https://github.com/kjhbond/claude-gpt-usage-tray/blob/main/docs/ACCOUNT.md",nullptr,nullptr,SW_SHOWNORMAL);
}
std::wstring AccountInfo(bool claude, const wchar_t* key) {
 wchar_t value[512];
 GetPrivateProfileStringW(L"Account",key,L"확인 불가",value,ARRAYSIZE(value),
  WidgetPath(claude?L"claude-info.ini":L"codex-info.ini").c_str());
 return value;
}
std::wstring ResetToolTip(bool claude) {
 auto tip=std::wstring(claude?L"Claude":L"ChatGPT")+L" · 주간 리셋: "+AccountInfo(claude,L"ResetLocal");
 auto status=AccountInfo(claude,L"StatusDisplay");
 if(status!=L"정상")tip+=L" · "+status;
 return tip;
}
Controls::Button MakeChatButton(Controls::StackPanel content, bool claude) {
 Controls::Button button;
 button.Name(claude?L"ClaudeQuotaButton":L"CodexQuotaButton");
 Automation::AutomationProperties::SetAutomationId(button,button.Name());
 Automation::AutomationProperties::SetName(button,claude?L"Claude 웹 채팅 열기":L"ChatGPT 웹 채팅 열기");
 button.Content(content);button.Padding({0,0,0,0});button.BorderThickness({0,0,0,0});
 button.MinWidth(0);button.MinHeight(0);button.Height(48);
 button.HorizontalContentAlignment(HorizontalAlignment::Center);button.VerticalContentAlignment(VerticalAlignment::Center);
 button.Background(Media::SolidColorBrush(winrt::Windows::UI::Color{0,0,0,0}));
 auto tip=ResetToolTip(claude);
 Controls::ToolTipService::SetToolTip(button,winrt::box_value(tip));
 (claude?g_claudeTip:g_codexTip)=tip;
 button.Click([claude](auto const&,auto const&){OpenWebChat(claude);});
 button.RightTapped([claude](winrt::Windows::Foundation::IInspectable const& sender,Input::RightTappedRoutedEventArgs const& args){
  args.Handled(true);
  try {
   auto owner=sender.as<FrameworkElement>();
   auto& menu=claude?g_claudeMenu:g_codexMenu;
   if(menu)menu.Hide();
   menu=Controls::MenuFlyout();
   auto label=[&](std::wstring const& value){Controls::MenuFlyoutItem item;item.Text(value);item.IsEnabled(false);menu.Items().Append(item);};
   label(claude?L"Claude — 주간 사용 한도":L"Codex — 주간 사용 한도");
   label(L"계정 ID: "+AccountInfo(claude,L"AccountId"));
   label(L"리셋 일시: "+AccountInfo(claude,L"ResetLocal"));
   label(L"조회 시각: "+AccountInfo(claude,L"CheckedLocal"));
   label(L"상태: "+AccountInfo(claude,L"StatusDisplay"));
   if(AccountInfo(claude,L"Status")==L"stale")label(L"마지막 정상 조회: "+AccountInfo(claude,L"LastSuccessLocal"));
   menu.Items().Append(Controls::MenuFlyoutSeparator());
   Controls::MenuFlyoutItem open;open.Text(claude?L"Claude 웹 채팅 열기":L"ChatGPT 웹 채팅 열기");
   open.Click([claude](auto const&,auto const&){OpenWebChat(claude);});menu.Items().Append(open);
   Controls::MenuFlyoutItem settings;settings.Text(L"표시 설정...");
   settings.Click([](auto const&,auto const&){OpenDisplaySettings();});menu.Items().Append(settings);
   Controls::MenuFlyoutItem help;help.Text(L"계정 연동 안내");
   help.Click([](auto const&,auto const&){OpenAccountHelp();});menu.Items().Append(help);
   menu.ShowAt(owner,args.GetPosition(owner));
  }catch(...){Wh_Log(L"Account menu unavailable");}
 });
 return button;
}


FrameworkElement FindNamed(DependencyObject node, const wchar_t* name) {
 auto e=node.try_as<FrameworkElement>();
 if(e && e.Name()==name)return e;
 int n=Media::VisualTreeHelper::GetChildrenCount(node);
 for(int i=0;i<n;i++)if(auto c=FindNamed(Media::VisualTreeHelper::GetChild(node,i),name))return c;
 return nullptr;
}
void RemoveWidget() {
 if(g_codexMenu){g_codexMenu.Hide();g_codexMenu=nullptr;}
 if(g_claudeMenu){g_claudeMenu.Hide();g_claudeMenu=nullptr;}
 if(g_grid && g_widget){
  uint32_t i;
  if(g_grid.Children().IndexOf(g_widget,i))g_grid.Children().RemoveAt(i);
  for(auto c:g_grid.Children())if(Controls::Grid::GetColumn(c.as<FrameworkElement>())>g_column)Controls::Grid::SetColumn(c.as<FrameworkElement>(),Controls::Grid::GetColumn(c.as<FrameworkElement>())-1);
  if(g_column>=0 && (uint32_t)g_column<g_grid.ColumnDefinitions().Size())g_grid.ColumnDefinitions().RemoveAt(g_column);
 }
 g_claudeText=nullptr;g_claudeLogo=nullptr;g_text=nullptr;g_logo=nullptr;g_codexButton=nullptr;g_claudeButton=nullptr;
 g_codexTip.clear();g_claudeTip.clear();g_widget=nullptr;g_grid=nullptr;g_column=-1;
}
void InstallWidget(XamlRoot root) {
 auto e=FindNamed(root.Content(),L"SystemTrayFrameGrid");if(!e)return;
 auto grid=e.try_as<Controls::Grid>();if(!grid)return;
 if(g_grid==grid && g_widget)return;
 RemoveWidget();
 auto indicator=FindNamed(grid,L"NonActivatableStack");if(!indicator)return;
 int col=Controls::Grid::GetColumn(indicator);
 Controls::StackPanel widget;
 widget.Name(L"CodexWeeklyQuotaWidget");
 widget.Orientation(Controls::Orientation::Horizontal);
 widget.VerticalAlignment(VerticalAlignment::Center);
 widget.Margin({8,0,8,0});
 Controls::Image logo;
 logo.Name(L"CodexOfficialChatGPTLogo");
 logo.Width(16);logo.Height(16);logo.VerticalAlignment(VerticalAlignment::Center);
 logo.Margin({0,0,5,0});
 std::ifstream f(std::filesystem::path(WidgetPath(L"chatgpt.png")),std::ios::binary);
 std::vector<uint8_t> bytes((std::istreambuf_iterator<char>(f)),{});
 if(!bytes.empty()){
  winrt::Windows::Storage::Streams::InMemoryRandomAccessStream stream;
  winrt::Windows::Storage::Streams::DataWriter writer(stream);
  writer.WriteBytes(bytes);writer.StoreAsync().get();writer.DetachStream();stream.Seek(0);
  Media::Imaging::BitmapImage bitmap;bitmap.SetSource(stream);logo.Source(bitmap);
 }
 Automation::AutomationProperties::SetName(logo,L"ChatGPT logo");
 Controls::TextBlock text;
 text.Name(L"CodexWeeklyQuotaText");text.Text(L"--%");
 text.FontFamily(Media::FontFamily(L"Segoe UI"));text.FontSize(16);
 text.VerticalAlignment(VerticalAlignment::Center);
 Documents::Typography::SetNumeralAlignment(text,FontNumeralAlignment::Proportional);
 Automation::AutomationProperties::SetAutomationId(text,L"CodexWeeklyQuotaText");
 Controls::StackPanel codexContent;codexContent.Orientation(Controls::Orientation::Horizontal);
 codexContent.Children().Append(logo);codexContent.Children().Append(text);
 auto codexButton=MakeChatButton(codexContent,false);widget.Children().Append(codexButton);
 Controls::Image claudeLogo;
 claudeLogo.Name(L"ClaudeOfficialLogo");
 claudeLogo.Width(16);claudeLogo.Height(16);claudeLogo.VerticalAlignment(VerticalAlignment::Center);
 claudeLogo.Margin({0,0,5,0});
 std::ifstream cf(std::filesystem::path(WidgetPath(L"claude.png")),std::ios::binary);
 std::vector<uint8_t> cbytes((std::istreambuf_iterator<char>(cf)),{});
 if(!cbytes.empty()){
  winrt::Windows::Storage::Streams::InMemoryRandomAccessStream stream;
  winrt::Windows::Storage::Streams::DataWriter writer(stream);
  writer.WriteBytes(cbytes);writer.StoreAsync().get();writer.DetachStream();stream.Seek(0);
  Media::Imaging::BitmapImage bitmap;bitmap.SetSource(stream);claudeLogo.Source(bitmap);
 }
 Automation::AutomationProperties::SetName(claudeLogo,L"Claude logo");
 Controls::TextBlock claudeText;
 claudeText.Name(L"ClaudeWeeklyQuotaText");claudeText.Text(L"--%");
 claudeText.FontFamily(Media::FontFamily(L"Segoe UI"));claudeText.FontSize(16);
 claudeText.VerticalAlignment(VerticalAlignment::Center);
 Documents::Typography::SetNumeralAlignment(claudeText,FontNumeralAlignment::Proportional);
 Automation::AutomationProperties::SetAutomationId(claudeText,L"ClaudeWeeklyQuotaText");
 Controls::StackPanel claudeContent;claudeContent.Orientation(Controls::Orientation::Horizontal);
 claudeContent.Children().Append(claudeLogo);claudeContent.Children().Append(claudeText);
 auto claudeButton=MakeChatButton(claudeContent,true);claudeButton.Margin({14,0,0,0});widget.Children().Append(claudeButton);
 g_claudeText=claudeText;g_claudeLogo=claudeLogo;g_codexButton=codexButton;g_claudeButton=claudeButton;

 Controls::ColumnDefinition column;column.Width({1,GridUnitType::Auto});
 grid.ColumnDefinitions().InsertAt(col,column);
 for(auto c:grid.Children())if(Controls::Grid::GetColumn(c.as<FrameworkElement>())>=col)Controls::Grid::SetColumn(c.as<FrameworkElement>(),Controls::Grid::GetColumn(c.as<FrameworkElement>())+1);
 Controls::Grid::SetColumn(widget,col);grid.Children().Append(widget);
 g_grid=grid;g_widget=widget;g_text=text;g_logo=logo;g_column=col;
}
void CALLBACK Tick(HWND,UINT,UINT_PTR,DWORD) {
 try {
  auto root=GetTaskbarXamlRoot(g_taskbar);if(!root)return;
  InstallWidget(root);if(!g_text)return;
  auto readDisplay=[](std::wstring const& path){
   WIN32_FILE_ATTRIBUTE_DATA attr{};bool fresh=false;
   if(GetFileAttributesExW(path.c_str(),GetFileExInfoStandard,&attr)){
    ULARGE_INTEGER t{};t.LowPart=attr.ftLastWriteTime.dwLowDateTime;t.HighPart=attr.ftLastWriteTime.dwHighDateTime;
    auto age=(long long)time(nullptr)-(long long)(t.QuadPart/10000000ULL-11644473600ULL);
    fresh=age>=-60 && age<300;
   }
   std::string value="--%";
   if(fresh){std::ifstream f{std::filesystem::path(path)};std::getline(f,value);}
   if(value.empty()||value.size()>5||value.find_first_not_of("0123456789%.-~")!=std::string::npos)value="--%";
   return value;
  };
  auto value=readDisplay(WidgetPath(L"display.txt"));
  auto claude=readDisplay(WidgetPath(L"claude-display.txt"));
  g_text.Text(winrt::to_hstring(value));g_claudeText.Text(winrt::to_hstring(claude));
  auto layout=WidgetPath(L"layout.ini");
  bool showCodex=GetPrivateProfileIntW(L"Layout",L"ShowCodex",1,layout.c_str())!=0;
  bool showClaude=GetPrivateProfileIntW(L"Layout",L"ShowClaude",1,layout.c_str())!=0;
  if(!showCodex&&!showClaude)showCodex=true;
  auto codexVisibility=showCodex?Visibility::Visible:Visibility::Collapsed;
  auto claudeVisibility=showClaude?Visibility::Visible:Visibility::Collapsed;
  if(g_codexButton.Visibility()!=codexVisibility)g_codexButton.Visibility(codexVisibility);
  if(g_claudeButton.Visibility()!=claudeVisibility)g_claudeButton.Visibility(claudeVisibility);
  float claudeLeft=showCodex?14.0f:0.0f;
  if(g_claudeButton.Margin().Left!=claudeLeft)g_claudeButton.Margin({claudeLeft,0,0,0});
  auto codexTip=ResetToolTip(false),claudeTip=ResetToolTip(true);
  if(codexTip!=g_codexTip){Controls::ToolTipService::SetToolTip(g_codexButton,winrt::box_value(codexTip));g_codexTip=codexTip;}
  if(claudeTip!=g_claudeTip){Controls::ToolTipService::SetToolTip(g_claudeButton,winrt::box_value(claudeTip));g_claudeTip=claudeTip;}
  std::string summary="Weekly quota remaining:";
  if(showCodex)summary+=" Codex "+value;
  if(showClaude){if(showCodex)summary+=",";summary+=" Claude "+claude;}
  Automation::AutomationProperties::SetName(g_widget,winrt::to_hstring(summary));
  double scale=root.RasterizationScale();
  // Physical glyph target is calibrated from an actual screenshot, independently of DPI.
  g_text.FontSize(GetPrivateProfileIntW(L"Layout",L"FontSizeHundredths",1700,layout.c_str())/100.0/scale);g_logo.Width(16.0/scale);g_logo.Height(16.0/scale);
  g_text.Margin({0,(int)GetPrivateProfileIntW(L"Layout",L"TopMarginHundredths",-200,layout.c_str())/100.0/scale,0,0});
  g_claudeText.FontSize(g_text.FontSize());g_claudeText.Margin(g_text.Margin());
  g_claudeLogo.Width(16.0/scale);g_claudeLogo.Height(16.0/scale);
 }catch(winrt::hresult_error const& e){Wh_Log(L"Quota widget: %s",e.message().c_str());}
 catch(...){Wh_Log(L"Quota widget failed");}
}
BOOL Wh_ModInit(){return HookTaskbarDllSymbols();}
void Wh_ModAfterInit(){
 g_taskbar=FindWindow(L"Shell_TrayWnd",nullptr);
 DWORD pid=0;GetWindowThreadProcessId(g_taskbar,&pid);
 if(pid!=GetCurrentProcessId())return;
 RunFromWindowThread(g_taskbar,[](void*){Tick(nullptr,0,0,0);g_timer=SetTimer(g_taskbar,0xC0DE120,1000,Tick);},nullptr);
}
void Wh_ModBeforeUninit(){if(g_timer)RunFromWindowThread(g_taskbar,[](void*){KillTimer(g_taskbar,0xC0DE120);g_timer=0;RemoveWidget();},nullptr);}
