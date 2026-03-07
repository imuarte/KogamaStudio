#define _SILENCE_EXPERIMENTAL_FILESYSTEM_DEPRECATION_WARNING
#include "Appearance.h"
#include "MenuHelpers.h"
#include "pipe.h"
#include <experimental/filesystem>
#include <fstream>
#include "json.hpp"

using namespace menu;
using json = nlohmann::json;
namespace fs = std::experimental::filesystem;

namespace Appearance {
    static int selectedTheme = 0;
    static float rounding = 0.0f;
    std::string fontPath = u8"";
    float fontSize = 13.0f;
    char fontPathBuf[256] = u8"";

    std::string GetConfigPath() {
        char* buffer = nullptr;
        size_t len;
        _dupenv_s(&buffer, &len, u8"APPDATA");
        std::string path = std::string(buffer) + u8"\\..\\Local\\KogamaStudio\\Config\\theme.json";
        free(buffer);
        return path;
    }

    void SaveSettings() {
        json config;
        config[u8"theme"] = selectedTheme;
        config[u8"rounding"] = rounding;
        config[u8"fontPath"] = fontPath;
        config[u8"fontSize"] = fontSize;
        config[u8"language"] = Locale::GetCurrentLanguage();

        ImGuiStyle& style = ImGui::GetStyle();
        json colors;
        for (int i = 0; i < ImGuiCol_COUNT; i++) {
            ImVec4 col = style.Colors[i];
            colors[std::to_string(i)] = { col.x, col.y, col.z, col.w };
        }
        config[u8"colors"] = colors;

        std::string path = GetConfigPath();
        fs::create_directories(fs::path(path).parent_path());

        std::ofstream file(path);
        file << config.dump(4);
        file.close();
    }

    void LoadSettings() {
        std::string path = GetConfigPath();
        if (fs::exists(path)) {
            try {
                json config = json::parse(std::ifstream(path));
                selectedTheme = config.value(u8"theme", 0);
                rounding = config.value(u8"rounding", 0.0f);
                fontPath = config.value(u8"fontPath", u8"");
                fontSize = config.value(u8"fontSize", 13.0f);
                strcpy_s(fontPathBuf, sizeof(fontPathBuf), fontPath.c_str());

                std::string lang = config.value(u8"language", u8"en");
                Locale::SetLanguage(lang.c_str());

                if (config.contains(u8"colors")) {
                    ImGuiStyle& style = ImGui::GetStyle();
                    json colors = config[u8"colors"];
                    for (auto it = colors.begin(); it != colors.end(); ++it) {
                        int idx = atoi(it.key().c_str());
                        if (idx >= 0 && idx < ImGuiCol_COUNT) {
                            style.Colors[idx] = ImVec4(
                                it.value()[0], it.value()[1], it.value()[2], it.value()[3]
                            );
                        }
                    }
                }

                ApplyTheme();
            }
            catch (...) {}
        }
    }

    void ApplyTheme() {
        ImGuiStyle& style = ImGui::GetStyle();
        ImGuiIO& io = ImGui::GetIO();

        if (selectedTheme == 0) ImGui::StyleColorsDark();
        else if (selectedTheme == 1) ImGui::StyleColorsLight();
        else if (selectedTheme == 2) ImGui::StyleColorsClassic();

        style.FrameRounding = rounding;
    }

    void Render() {
        ImGui::SetNextWindowSize(ImVec2(400, 600), ImGuiCond_FirstUseEver);
        ImGui::SetNextWindowPos(ImVec2(1110, 20), ImGuiCond_FirstUseEver);
        ImGuiWindowFlags newFlags = ImGuiWindowFlags_NoCollapse;
        if (ImGui::Begin((std::string(T(u8"Appearance")) + u8"###Appearance").c_str(), nullptr, newFlags))
        {
            ImGui::Text(T(u8"Theme:"));
            if (ImGui::RadioButton(T(u8"Dark"), &selectedTheme, 0)) {
                ApplyTheme();
                SaveSettings();
            }
            if (ImGui::RadioButton(T(u8"Light"), &selectedTheme, 1)) {
                ApplyTheme();
                SaveSettings();
            }
            if (ImGui::RadioButton(T(u8"Classic"), &selectedTheme, 2)) {
                ApplyTheme();
                SaveSettings();
            }
            if (ImGui::RadioButton(T(u8"Custom"), &selectedTheme, 3)) {
                SaveSettings();
            }
            ImGui::Separator();

            ImGuiStyle& style = ImGui::GetStyle();
            ImGuiIO& io = ImGui::GetIO();

            if (ImGui::InputText(T(u8"Font Path"), fontPathBuf, sizeof(fontPathBuf))) {
                fontPath = fontPathBuf;
                SaveSettings();
            }

            if (ImGui::SliderFloat(T(u8"Font Size"), &fontSize, 8.0f, 24.0f)) {
                SaveSettings();
            }

            if (ImGui::SliderFloat(T(u8"Rounding"), &rounding, 0.0f, 10.0f)) {
                style.FrameRounding = rounding;
                SaveSettings();
            }
            ImGui::Separator();

            if (selectedTheme == 3) {
                ImGui::BeginChild(u8"ColorScroll", ImVec2(0, 300), true);
                for (int i = 0; i < ImGuiCol_COUNT; i++) {
                    const char* colorName = ImGui::GetStyleColorName(i);
                    if (ImGui::ColorEdit4(colorName, (float*)&style.Colors[i], ImGuiColorEditFlags_NoInputs)) {
                        SaveSettings();
                    }
                }
                ImGui::EndChild();
            }

            ImGui::Separator();

            ImGui::Text(T(u8"Language:"));

            if (ImGui::Button(T(u8"Refresh"))) {
                Locale::RefreshLanguageList();
            }
            ImGui::SameLine();
            ImGui::TextDisabled(u8"(%LOCALAPPDATA%\\KogamaStudio\\Lang\\)");

            int langCount = Locale::GetLanguageCount();
            for (int i = 0; i < langCount; i++) {
                const char* lang = Locale::GetLanguageName(i);
                bool sel = (strcmp(lang, Locale::GetCurrentLanguage()) == 0);
                if (ImGui::RadioButton(lang, sel)) {
                    Locale::SetLanguage(lang);
                    SaveSettings();
                }
            }

            ImGui::Separator();

            if (ImGui::Button(T(u8"Reset"), ImVec2(-1, 0))) {
                selectedTheme = 0;
                rounding = 0.0f;
                fontPath = u8"";
                fontSize = 13.0f;
                memset(fontPathBuf, 0, sizeof(fontPathBuf));
                ApplyTheme();
                SaveSettings();
            }
        }
        ImGui::End();
    }
}
