#include "stdafx.h"
#include "pipe.h"
#include "Appearance.h"

namespace hooks_dx11 {
    using Microsoft::WRL::ComPtr;

    PresentFn       oPresentD3D11 = nullptr;
    Present1Fn      oPresent1D3D11 = nullptr;
    ResizeBuffersFn oResizeBuffersD3D11 = nullptr;
    static void* pPresentTarget = nullptr;
    static void* pPresent1Target = nullptr;
    static void* pResizeBuffersTarget = nullptr;

    ID3D11Device* gDevice = nullptr;
    static ID3D11DeviceContext* gContext = nullptr;
    static IDXGISwapChain* gSwapChain = nullptr;
    static ID3D11RenderTargetView* gRTV = nullptr;
    static bool                     gInitialized = false;
    static ID3D11Texture2D*          gGameFrameTexture = nullptr;
    static ID3D11ShaderResourceView* gGameFrameSRV = nullptr;

    static DXGI_FORMAT StripSRGB(DXGI_FORMAT fmt)
    {
        if (fmt == DXGI_FORMAT_R8G8B8A8_UNORM_SRGB) return DXGI_FORMAT_R8G8B8A8_UNORM;
        if (fmt == DXGI_FORMAT_B8G8R8A8_UNORM_SRGB)  return DXGI_FORMAT_B8G8R8A8_UNORM;
        return fmt;
    }

    static void CreateGameFrameTexture()
    {
        ID3D11Texture2D* pBB = nullptr;
        if (!gSwapChain || FAILED(gSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBB)))) return;
        D3D11_TEXTURE2D_DESC bbDesc;
        pBB->GetDesc(&bbDesc);
        pBB->Release();
        D3D11_TEXTURE2D_DESC td = bbDesc;
        td.BindFlags          = D3D11_BIND_SHADER_RESOURCE;
        td.Usage              = D3D11_USAGE_DEFAULT;
        td.MiscFlags          = 0;
        td.SampleDesc.Count   = 1;
        td.SampleDesc.Quality = 0;
        if (FAILED(gDevice->CreateTexture2D(&td, nullptr, &gGameFrameTexture))) return;
        D3D11_SHADER_RESOURCE_VIEW_DESC srvd = {};
        srvd.Format              = td.Format;
        srvd.ViewDimension       = D3D11_SRV_DIMENSION_TEXTURE2D;
        srvd.Texture2D.MipLevels = 1;
        gDevice->CreateShaderResourceView(gGameFrameTexture, &srvd, &gGameFrameSRV);
    }

    static void CleanupGameFrameTexture()
    {
        if (gGameFrameSRV)     { gGameFrameSRV->Release();     gGameFrameSRV = nullptr; }
        if (gGameFrameTexture) { gGameFrameTexture->Release(); gGameFrameTexture = nullptr; }
    }

    static void CreateRenderTarget()
    {
        ID3D11Texture2D* pBackBuffer = nullptr;
        if (gSwapChain && SUCCEEDED(gSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBackBuffer))))
        {
            HRESULT hr = gDevice->CreateRenderTargetView(pBackBuffer, nullptr, &gRTV);
            pBackBuffer->Release();
            if (SUCCEEDED(hr))
            {
                DebugLog("[d3d11hook] CreateRenderTarget success RTV@%p\n", gRTV);
            }
            else
            {
                DebugLog("[d3d11hook] CreateRenderTarget failed: 0x%08X\n", hr);
            }
        }
        else
        {
            DebugLog("[d3d11hook] CreateRenderTarget failed: swapchain buffer unavailable\n");
        }
    }

    static void CleanupRenderTarget()
    {
        if (gRTV)
        {
            gRTV->Release();
            gRTV = nullptr;
        }
    }

    static void RenderFrame(IDXGISwapChain* pSwapChain)
    {
        static bool pipeInitialized = false;
        if (!pipeInitialized)
        {
            pipe::ListenForCommands();
            pipeInitialized = true;
        }

        static bool loggedPresent = false;
        if (!loggedPresent)
        {
            DebugLog("[d3d11hook] RenderFrame via Present hook %p\n", oPresentD3D11);
            loggedPresent = true;
        }

        if (!gInitialized)
        {
            gSwapChain = pSwapChain;
            if (SUCCEEDED(pSwapChain->GetDevice(__uuidof(ID3D11Device), (void**)&gDevice)))
            {
                gDevice->GetImmediateContext(&gContext);

                DXGI_SWAP_CHAIN_DESC desc{};
                pSwapChain->GetDesc(&desc);

                ImGui::CreateContext();
                ImGuiIO& io = ImGui::GetIO(); (void)io;


                Appearance::LoadSettings();
                static const ImWchar glyphRanges[] = {
                    0x0020, 0x00FF, // Basic Latin + Latin-1 Supplement
                    0x0100, 0x017F, // Latin Extended-A
                    0x0180, 0x024F, // Latin Extended-B
                    0x0250, 0x02AF, // IPA Extensions
                    0x02B0, 0x02FF, // Spacing Modifier Letters (˚)
                    0x0300, 0x036F, // Combining Diacritical Marks
                    0x0370, 0x03FF, // Greek and Coptic
                    0x0400, 0x04FF, // Cyrillic
                    0x0500, 0x052F, // Cyrillic Supplement
                    0x1E00, 0x1EFF, // Latin Extended Additional
                    0x2000, 0x206F, // General Punctuation
                    0x20A0, 0x20CF, // Currency Symbols
                    0x2100, 0x214F, // Letterlike Symbols
                    0x2190, 0x21FF, // Arrows
                    0x2200, 0x22FF, // Mathematical Operators (∘)
                    0x25A0, 0x25FF, // Geometric Shapes
                    0x2600, 0x26FF, // Miscellaneous Symbols
                    0,
                };
                bool fontLoaded = false;
                if (!Appearance::fontPath.empty() && Appearance::fontSize > 0) {
                    FILE* f = nullptr;
                    if (fopen_s(&f, Appearance::fontPath.c_str(), "rb") == 0 && f != nullptr) {
                        fclose(f);
                        fontLoaded = (io.Fonts->AddFontFromFileTTF(Appearance::fontPath.c_str(), Appearance::fontSize, nullptr, glyphRanges) != nullptr);
                    }
                }
                if (!fontLoaded) {
                    fontLoaded = (io.Fonts->AddFontFromFileTTF("C:\\Windows\\Fonts\\consola.ttf", 13.0f, nullptr, glyphRanges) != nullptr);
                }
                if (!fontLoaded) {
                    io.Fonts->AddFontDefaultVector();
                }
                io.Fonts->Build();

                io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;
                io.ConfigFlags |= ImGuiConfigFlags_DockingEnable;
                io.ConfigFlags |= ImGuiConfigFlags_ViewportsEnable;
                ImGui::StyleColorsDark();
                ImGuiStyle& style = ImGui::GetStyle();
                style.WindowRounding = 0.0f;
                style.Colors[ImGuiCol_WindowBg].w = 1.0f;
                ImGui_ImplWin32_Init(desc.OutputWindow);
                ImGui_ImplDX11_Init(gDevice, gContext);
                inputhook::Init(desc.OutputWindow);
                inputhook::InitGetCursorPosHook();
                CreateRenderTarget();
                CreateGameFrameTexture();
                gInitialized = true;
                DebugLog("[d3d11hook] ImGui initialized.\n");
            }
        }

        if (GetAsyncKeyState(globals::openMenuKey) & 1)
        {
            menu::isOpen = !menu::isOpen;
            DebugLog("[d3d11hook] Toggle menu: %d\n", menu::isOpen);
        }

        if (GetAsyncKeyState(globals::uninjectKey) & 1)
        {
            Uninject();
            return;
        }

        if (gInitialized)
        {
            if (menu::isOpen && pipe::openMenu)
            {
                ID3D11Texture2D* pBB = nullptr;
                if (gSwapChain && SUCCEEDED(gSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBB))))
                {
                    if (gGameFrameTexture)
                    {
                        D3D11_TEXTURE2D_DESC bbDesc, texDesc;
                        pBB->GetDesc(&bbDesc);
                        gGameFrameTexture->GetDesc(&texDesc);
                        if (bbDesc.Width != texDesc.Width || bbDesc.Height != texDesc.Height)
                        {
                            CleanupGameFrameTexture();
                            CreateGameFrameTexture();
                        }
                        if (gGameFrameTexture)
                        {
                            if (bbDesc.SampleDesc.Count > 1)
                                gContext->ResolveSubresource(gGameFrameTexture, 0, pBB, 0, bbDesc.Format);
                            else
                                gContext->CopyResource(gGameFrameTexture, pBB);
                        }
                    }
                    pBB->Release();
                }
                const float clearColor[4] = { 0.06f, 0.06f, 0.06f, 1.0f };
                gContext->OMSetRenderTargets(1, &gRTV, nullptr);
                gContext->ClearRenderTargetView(gRTV, clearColor);
            }

            inputhook::remapCursor = false;
            ImGui_ImplDX11_NewFrame();
            ImGui_ImplWin32_NewFrame();
            ImGui::NewFrame();

            ImGuiIO& io = ImGui::GetIO();
            
            menu::Init();

            io.WantCaptureMouse = ImGui::IsAnyItemHovered() && !menu::viewportHovered;



            ImGui::EndFrame();
            ImGui::Render();
            gContext->OMSetRenderTargets(1, &gRTV, nullptr);
            if (gContext)
            {
                ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
                if (io.ConfigFlags & ImGuiConfigFlags_ViewportsEnable)
                {
                    ImGui::UpdatePlatformWindows();
                    ImGui::RenderPlatformWindowsDefault();
                }
            }
            else
            {
                DebugLog("[d3d11hook] Render skipped: device context invalid\n");
            }
            inputhook::remapCursor = true;
        }
    }

    HRESULT __stdcall hookPresentD3D11(IDXGISwapChain* pSwapChain, UINT SyncInterval, UINT Flags)
    {
        RenderFrame(pSwapChain);
        return oPresentD3D11(pSwapChain, SyncInterval, Flags);
    }

    HRESULT __stdcall hookPresent1D3D11(IDXGISwapChain1* pSwapChain, UINT SyncInterval, UINT Flags, const DXGI_PRESENT_PARAMETERS* pPresentParameters)
    {
        RenderFrame(pSwapChain);
        return oPresent1D3D11(pSwapChain, SyncInterval, Flags, pPresentParameters);
    }

    HRESULT __stdcall hookResizeBuffersD3D11(
        IDXGISwapChain* pSwapChain,
        UINT BufferCount,
        UINT Width,
        UINT Height,
        DXGI_FORMAT NewFormat,
        UINT SwapChainFlags)
    {
        if (gInitialized)
        {
            ImGui_ImplDX11_InvalidateDeviceObjects();
            CleanupGameFrameTexture();
            CleanupRenderTarget();
        }

        HRESULT hr = oResizeBuffersD3D11(pSwapChain, BufferCount, Width, Height, NewFormat, SwapChainFlags);

        if (gInitialized)
        {
            DebugLog("[d3d11hook] ResizeBuffers: recreating render target\n");
            CreateRenderTarget();
            CreateGameFrameTexture();
            ImGui_ImplDX11_CreateDeviceObjects();
        }

        return hr;
    }

    void Init()
    {
        DebugLog("[d3d11hook] Init starting\n");

        WNDCLASSEXW wc = {
            sizeof(WNDCLASSEXW), CS_CLASSDC, DefWindowProcW,
            0L, 0L, GetModuleHandleW(nullptr), nullptr, nullptr, nullptr, nullptr,
            L"DummyDX11", nullptr
        };
        RegisterClassExW(&wc);
        HWND hwnd = CreateWindowW(wc.lpszClassName, L"", WS_OVERLAPPEDWINDOW,
            0, 0, 100, 100, nullptr, nullptr, wc.hInstance, nullptr);

        DXGI_SWAP_CHAIN_DESC sd{};
        sd.BufferCount = 2;
        sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
        sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
        sd.OutputWindow = hwnd;
        sd.SampleDesc.Count = 1;
        sd.Windowed = TRUE;
        sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

        D3D_FEATURE_LEVEL featureLevel = D3D_FEATURE_LEVEL_11_0;
        ID3D11Device* device = nullptr;
        ID3D11DeviceContext* context = nullptr;
        IDXGISwapChain* swapChain = nullptr;

        HRESULT hr = D3D11CreateDeviceAndSwapChain(
            nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0, &featureLevel, 1,
            D3D11_SDK_VERSION, &sd, &swapChain, &device, nullptr, &context);

        if (SUCCEEDED(hr))
        {
            void** vtbl = *reinterpret_cast<void***>(swapChain);
            pPresentTarget = vtbl[8];
            pResizeBuffersTarget = vtbl[13];
            MH_CreateHook(pPresentTarget, hookPresentD3D11, reinterpret_cast<void**>(&oPresentD3D11));
            MH_CreateHook(pResizeBuffersTarget, hookResizeBuffersD3D11, reinterpret_cast<void**>(&oResizeBuffersD3D11));

            IDXGISwapChain1* swapChain1 = nullptr;
            void* present1Addr = nullptr;
            if (SUCCEEDED(swapChain->QueryInterface(__uuidof(IDXGISwapChain1), (void**)&swapChain1)))
            {
                void** vtbl1 = *reinterpret_cast<void***>(swapChain1);
                present1Addr = vtbl1[22];
                pPresent1Target = vtbl1[22];
                MH_CreateHook(pPresent1Target, hookPresent1D3D11, reinterpret_cast<void**>(&oPresent1D3D11));
                MH_EnableHook(pPresent1Target);
                swapChain1->Release();
            }

            MH_EnableHook(pPresentTarget);
            MH_EnableHook(pResizeBuffersTarget);
            DebugLog("[d3d11hook] Hooks placed Present@%p Present1@%p ResizeBuffers@%p\n", pPresentTarget, present1Addr, pResizeBuffersTarget);
            swapChain->Release();
            device->Release();
            context->Release();
        }
        else
        {
            DebugLog("[d3d11hook] D3D11CreateDeviceAndSwapChain failed: 0x%08X\n", hr);
        }

        DestroyWindow(hwnd);
        UnregisterClassW(wc.lpszClassName, wc.hInstance);
    }

    void release()
    {
        DebugLog("[d3d11hook] Releasing resources\n");

        inputhook::RemoveGetCursorPosHook();
        if (globals::mainWindow)
            inputhook::Remove(globals::mainWindow);

        if (gInitialized)
        {
            ImGui_ImplDX11_Shutdown();
            ImGui_ImplWin32_Shutdown();
            ImGui::DestroyContext();
            CleanupGameFrameTexture();
            CleanupRenderTarget();
            if (gContext)
            {
                gContext->Release();
                gContext = nullptr;
            }
            if (gDevice)
            {
                gDevice->Release();
                gDevice = nullptr;
            }
            gSwapChain = nullptr;
            gInitialized = false;
        }

        if (pPresentTarget) {
            MH_DisableHook(pPresentTarget);
            MH_RemoveHook(pPresentTarget);
            pPresentTarget = nullptr;
        }
        if (pPresent1Target) {
            MH_DisableHook(pPresent1Target);
            MH_RemoveHook(pPresent1Target);
            pPresent1Target = nullptr;
        }
        if (pResizeBuffersTarget) {
            MH_DisableHook(pResizeBuffersTarget);
            MH_RemoveHook(pResizeBuffersTarget);
            pResizeBuffersTarget = nullptr;
        }
    }

    bool IsInitialized()
    {
        return gInitialized;
    }

    ID3D11ShaderResourceView* GetGameFrameSRV()
    {
        return gGameFrameSRV;
    }

    void GetGameFrameSize(UINT& width, UINT& height)
    {
        if (gGameFrameTexture)
        {
            D3D11_TEXTURE2D_DESC desc;
            gGameFrameTexture->GetDesc(&desc);
            width  = desc.Width;
            height = desc.Height;
        }
        else
        {
            width  = 0;
            height = 0;
        }
    }
}
