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
    std::string fontPath = "";
    float fontSize = 13.0f;
    char fontPathBuf[256] = "";

    std::string GetConfigPath() {
        char* buffer = nullptr;
        size_t len;
        _dupenv_s(&buffer, &len, "APPDATA");
        std::string path = std::string(buffer) + "\\..\\Local\\KogamaStudio\\Config\\theme.json";
        free(buffer);
        return path;
    }

    void SaveSettings() {
        json config;
        config["theme"] = selectedTheme;
        config["rounding"] = rounding;
        config["fontPath"] = fontPath;
        config["fontSize"] = fontSize;

        ImGuiStyle& style = ImGui::GetStyle();
        json colors;
        for (int i = 0; i < ImGuiCol_COUNT; i++) {
            ImVec4 col = style.Colors[i];
            colors[std::to_string(i)] = { col.x, col.y, col.z, col.w };
        }
        config["colors"] = colors;

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
                selectedTheme = config.value("theme", 0);
                rounding = config.value("rounding", 0.0f);
                fontPath = config.value("fontPath", "");
                fontSize = config.value("fontSize", 13.0f);
                strcpy_s(fontPathBuf, sizeof(fontPathBuf), fontPath.c_str());

                if (config.contains("colors")) {
                    ImGuiStyle& style = ImGui::GetStyle();
                    json colors = config["colors"];
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
        if (ImGui::Begin("Appearance", nullptr, newFlags))
        {
            ImGui::Text("Theme:");
            if (ImGui::RadioButton("Dark", &selectedTheme, 0)) {
                ApplyTheme();
                SaveSettings();
            }
            if (ImGui::RadioButton("Light", &selectedTheme, 1)) {
                ApplyTheme();
                SaveSettings();
            }
            if (ImGui::RadioButton("Classic", &selectedTheme, 2)) {
                ApplyTheme();
                SaveSettings();
            }
            if (ImGui::RadioButton("Custom", &selectedTheme, 3)) {
                SaveSettings();
            }
            ImGui::Separator();

            ImGuiStyle& style = ImGui::GetStyle();
            ImGuiIO& io = ImGui::GetIO();

            if (ImGui::InputText("Font Path", fontPathBuf, sizeof(fontPathBuf))) {
                fontPath = fontPathBuf;
                SaveSettings();
            }

            if (ImGui::SliderFloat("Font Size", &fontSize, 8.0f, 24.0f)) {
                SaveSettings();
            }

            if (ImGui::SliderFloat("Rounding", &rounding, 0.0f, 10.0f)) {
                style.FrameRounding = rounding;
                SaveSettings();
            }
            ImGui::Separator();

            if (selectedTheme == 3) {
                ImGui::BeginChild("ColorScroll", ImVec2(0, 300), true);
                for (int i = 0; i < ImGuiCol_COUNT; i++) {
                    const char* colorName = ImGui::GetStyleColorName(i);
                    if (ImGui::ColorEdit4(colorName, (float*)&style.Colors[i], ImGuiColorEditFlags_NoInputs)) {
                        SaveSettings();
                    }
                }
                ImGui::EndChild();
            }

            ImGui::Separator();

            if (ImGui::Button("Reset", ImVec2(-1, 0))) {
                selectedTheme = 0;
                rounding = 0.0f;
                fontPath = "";
                fontSize = 13.0f;
                memset(fontPathBuf, 0, sizeof(fontPathBuf));
                ApplyTheme();
                SaveSettings();
            }
        }
        ImGui::End();
    }
}