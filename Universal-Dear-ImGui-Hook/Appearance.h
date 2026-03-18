#pragma once
#include "stdafx.h"
#include <iostream>
namespace Appearance {
    void Render();
    void LoadSettings();
    void SaveSettings();
    void ApplyTheme();
    std::string GetConfigPath();

    extern bool appearanceOpen;
    extern int selectedTheme;
    extern float rounding;
    extern bool showColorEditor;
    extern std::string fontPath;
    extern char fontPathBuf[256];
    extern float fontSize;

    // Large font for headers (set during init, may be null)
    extern ImFont* headerFont;
}