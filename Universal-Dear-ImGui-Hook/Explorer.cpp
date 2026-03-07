#include "stdafx.h"
#include "Explorer.h"
#include "MenuHelpers.h"
#include "UILocale.h"
#include "json.hpp"
#include <fstream>
#include <cstdlib>

using json = nlohmann::json;
using namespace menu;

namespace Explorer {

    static std::vector<ObjectEntry> objectList;
    static char filterBuf[128] = "";

    void LoadFromFile()
    {
        char* appdata = nullptr;
        size_t len = 0;
        _dupenv_s(&appdata, &len, "LOCALAPPDATA");
        if (!appdata) return;

        std::string path = std::string(appdata) + "\\KogamaStudio\\explorer_objects.json";
        free(appdata);
        std::ifstream file(path);
        if (!file.is_open()) return;

        auto j = json::parse(file, nullptr, false);
        if (j.is_discarded() || !j.is_array()) return;

        objectList.clear();
        for (auto& item : j) {
            ObjectEntry obj;
            obj.id   = std::to_string(item.value("id", 0));
            obj.type = item.value("type", "");
            if (!obj.id.empty() && obj.id != "0")
                objectList.push_back(obj);
        }
    }

    void Render()
    {
        ImGuiWindowFlags flags = ImGuiWindowFlags_NoCollapse;
        if (ImGui::Begin((std::string(T(u8"Explorer")) + u8"###Explorer").c_str(), nullptr, flags))
        {
            if (ImGui::Button(T(u8"Refresh")))
                SendCommand(u8"explorer_refresh");

            ImGui::SameLine();
            ImGui::SetNextItemWidth(-1);
            ImGui::InputText(u8"##explorer_filter", filterBuf, sizeof(filterBuf));
            if (!typing) typing = ImGui::IsItemActive();

            ImGui::Separator();

            std::string countLabel = std::string(T(u8"Objects")) + u8": " + std::to_string(objectList.size());
            ImGui::TextDisabled("%s", countLabel.c_str());

            ImGui::BeginChild(u8"##explorer_list", ImVec2(0, 0), false);

            if (objectList.empty()) {
                ImGui::TextDisabled(T(u8"No objects"));
            } else {
                std::string filter = filterBuf;
                for (const auto& obj : objectList) {
                    std::string label = obj.id + u8" (" + obj.type + u8")";
                    if (!filter.empty() && label.find(filter) == std::string::npos)
                        continue;

                    if (ImGui::Selectable(label.c_str()))
                        SendCommand((u8"explorer_select|" + obj.id).c_str());
                }
            }

            ImGui::EndChild();
        }
        ImGui::End();
    }

}
