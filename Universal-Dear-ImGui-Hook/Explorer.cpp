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
    static std::string selectedId;
    static bool scrollToSelected = false;

    void SetSelected(const std::string& id)
    {
        selectedId = id;
        scrollToSelected = true;
    }

    void LoadFromFile(const std::string& path)
    {
        std::ifstream file(path);
        if (!file.is_open()) return;

        auto j = json::parse(file, nullptr, false);
        if (j.is_discarded() || !j.is_array()) return;

        objectList.clear();
        for (auto& item : j) {
            ObjectEntry obj;
            obj.id   = std::to_string(item.value("id", 0));
            obj.type = item.value("type", "");
            obj.name = item.value("name", "");
            if (!obj.id.empty() && obj.id != "0")
                objectList.push_back(obj);
        }
    }

    void Render()
    {
        ImGuiWindowFlags flags = ImGuiWindowFlags_NoCollapse;
        if (ImGui::Begin((std::string(T(u8"Explorer")) + u8"###Explorer").c_str(), nullptr, flags))
        {
            if (ImGui::Button(T(u8"Refresh"))) SendCommand(u8"explorer_refresh");

            ImGui::SameLine();
            ImGui::SetNextItemWidth(-1);
            ImGui::InputText(u8"##explorer_filter", filterBuf, sizeof(filterBuf));
            if (!typing) typing = ImGui::IsItemActive();

            ImGui::Separator();

            ImGui::TextDisabled("%s: %zu", T(u8"Objects"), objectList.size());

            ImGui::BeginChild(u8"##explorer_list", ImVec2(0, 0), false);

            if (objectList.empty()) {
                ImGui::TextDisabled(T(u8"No objects"));
            } else {
                std::string filter = filterBuf;
                for (const auto& obj : objectList) {
                    std::string display = obj.name.empty() ? obj.type : obj.name;
                    std::string label = display + u8"##" + obj.id;
                    if (!filter.empty() && display.find(filter) == std::string::npos && obj.id.find(filter) == std::string::npos)
                        continue;

                    bool isSelected = (obj.id == selectedId);
                    if (ImGui::Selectable(label.c_str(), isSelected)) {
                        selectedId = obj.id;
                        scrollToSelected = false;
                        SendCommand((u8"explorer_select|" + obj.id).c_str());
                    }
                    if (isSelected && scrollToSelected) {
                        ImGui::SetScrollHereY(0.5f);
                        scrollToSelected = false;
                    }
                    if (ImGui::IsItemHovered())
                        ImGui::SetTooltip(u8"ID: %s", obj.id.c_str());
                }
            }

            ImGui::EndChild();
        }
        ImGui::End();
    }

}
