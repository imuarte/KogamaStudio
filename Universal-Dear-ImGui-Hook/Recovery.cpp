#include "Recovery.h"
#include "MenuHelpers.h"
#include "pipe.h"
#include "Inventory.h"

using namespace menu;

namespace Recovery {

    bool recoveryOpen = false;
    int targetItemId = -1;
    bool corruptionDetected = false;
    bool corruptionDismissed = false;

    static std::vector<Problem> problems;
    static int currentPage = 0; // 0 = advice page, 1 = problems list

    void AddProblem(const std::string& objectId, const std::string& actionType, const std::string& description)
    {
        problems.push_back({ description, objectId, actionType });
    }

    static void RenderCorruptionPopup()
    {
        if (!corruptionDetected || corruptionDismissed) return;

        ImGui::SetNextWindowSize(ImVec2(520, 0), ImGuiCond_FirstUseEver);
        ImGui::SetNextWindowPos(ImVec2(ImGui::GetIO().DisplaySize.x * 0.5f, ImGui::GetIO().DisplaySize.y * 0.5f),
            ImGuiCond_FirstUseEver, ImVec2(0.5f, 0.5f));

        ImGuiWindowFlags flags = ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoDocking;

        bool open = true;
        if (ImGui::Begin((std::string(T(u8"Corrupted World Data Detected")) + u8"###CorruptionWarning").c_str(), &open, flags))
        {
            int count = (int)problems.size();
            if (count > 0)
                ImGui::Text(T(u8"Found %d problem(s)"), count);

            ImGui::Separator();
            ImGui::Spacing();

            if (currentPage == 0)
            {
                ImGui::TextWrapped(T(u8"One or more world objects could not be loaded due to corrupted data. The game has continued loading with the damaged content skipped. Your world may appear incomplete or behave unexpectedly."));

                ImGui::Spacing();
                ImGui::Separator();
                ImGui::Spacing();

                ImGui::TextUnformatted(T(u8"You can try to recover your world manually:"));
                ImGui::Spacing();

                ImGui::Bullet(); ImGui::SameLine(); ImGui::TextWrapped(T(u8"Camera unresponsive? Enable Freecam in the View tab."));
                ImGui::Bullet(); ImGui::SameLine(); ImGui::TextWrapped(T(u8"Use the Explorer panel to browse and select objects."));
                ImGui::Bullet(); ImGui::SameLine(); ImGui::TextWrapped(T(u8"Use the Properties panel to reposition or delete problematic objects."));
                ImGui::Bullet(); ImGui::SameLine(); ImGui::TextWrapped(T(u8"Use the Recovery panel for advanced tools (e.g. inventory item removal)."));

                ImGui::Spacing();
                ImGui::Separator();
                ImGui::Spacing();

                if (ImGui::Button(T(u8"I understand"))) corruptionDismissed = true;
                if (count > 0)
                {
                    ImGui::SameLine();
                    if (ImGui::Button(T(u8"Next >"))) currentPage = 1;
                }
            }
            else
            {
                ImGui::BeginChild(u8"##problems_list", ImVec2(0, -40), false);
                for (int i = 0; i < count; i++)
                {
                    const Problem& p = problems[i];
                    ImGui::PushID(i);

                    ImGui::TextWrapped("%s", p.description.c_str());

                    if (p.actionType == "remove" && !p.objectId.empty())
                    {
                        if (ImGui::Button(T(u8"Remove Object")))
                            SendCommand((std::string(u8"recovery_remove_object|") + p.objectId).c_str());
                    }
                    else if (p.actionType == "teleport" && !p.objectId.empty())
                    {
                        if (ImGui::Button(T(u8"Teleport to Object")))
                            SendCommand((std::string(u8"recovery_teleport_to_object|") + p.objectId).c_str());
                    }

                    if (i < count - 1) ImGui::Separator();
                    ImGui::PopID();
                }
                ImGui::EndChild();

                ImGui::Separator();
                if (ImGui::Button(T(u8"< Back"))) currentPage = 0;
                ImGui::SameLine();
                if (ImGui::Button(T(u8"Close"))) corruptionDismissed = true;
            }
        }
        ImGui::End();
        if (!open) corruptionDismissed = true;
    }

    void RenderOverlay() {
        RenderCorruptionPopup();
    }

    void Render() {

        if (recoveryOpen) {
            ImGuiWindowFlags flags = ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoCollapse;
            ImGui::SetNextWindowSize(ImVec2(450, 600), ImGuiCond_FirstUseEver);
            ImGui::SetNextWindowPos(ImVec2((1920 / 2) - (450 / 2), (1080 / 2) - (600 / 2)), ImGuiCond_FirstUseEver);

            if (ImGui::Begin((std::string(T(u8"Recovery")) + u8"###Recovery").c_str(), nullptr))
            {
                if (ImGui::BeginTabBar(u8"Tabs"))
                {
                    if (ImGui::BeginTabItem(T(u8"Tools")))
                    {
                        menu::IntInput(T(u8"Target ItemID"), targetItemId, u8"recovery_target_item_id", typing);

                        if (ImGui::Button(T(u8"Remove Item From Inventory"))) SendCommand(u8"recovery_remove_item");

                        if (ImGui::Button(T(u8"Disable Loading Screen"))) SendCommand(u8"recovery_disable_loading_screen");
                        ImGui::EndTabItem();
                    }
                    if (ImGui::BeginTabItem(T(u8"Fixes")))
                    {
                        ImGui::EndTabItem();
                    }
                    if (ImGui::BeginTabItem(T(u8"Danger Zone")))
                    {
                        static std::string selectedCatName;
                        static std::string selectedCatId;
                        static bool confirmRemoveCat = false;

                        const auto& cats = Inventory::GetCategories();
                        if (cats.empty())
                        {
                            ImGui::TextDisabled(T(u8"No inventory loaded. Refresh inventory first."));
                        }
                        else
                        {
                            if (cats.find(selectedCatName) == cats.end()) { selectedCatName = cats.begin()->first; selectedCatId = cats.begin()->second.empty() ? "" : cats.begin()->second[0].categoryId; }

                            ImGui::SetNextItemWidth(-1);
                            if (ImGui::BeginCombo(u8"##removecatcombo", (selectedCatName + " (" + std::to_string((int)cats.at(selectedCatName).size()) + ")").c_str()))
                            {
                                for (const auto& kv : cats)
                                {
                                    bool sel = (kv.first == selectedCatName);
                                    if (ImGui::Selectable((kv.first + " (" + std::to_string((int)kv.second.size()) + ")##rc" + kv.first).c_str(), sel)) { selectedCatName = kv.first; selectedCatId = kv.second.empty() ? "" : kv.second[0].categoryId; confirmRemoveCat = false; }
                                    if (sel) ImGui::SetItemDefaultFocus();
                                }
                                ImGui::EndCombo();
                            }

                            if (!confirmRemoveCat)
                            {
                                ImGui::PushStyleColor(ImGuiCol_Button, ImVec4(0.6f, 0.1f, 0.1f, 1.f));
                                ImGui::PushStyleColor(ImGuiCol_ButtonHovered, ImVec4(0.8f, 0.2f, 0.2f, 1.f));
                                ImGui::PushStyleColor(ImGuiCol_ButtonActive, ImVec4(0.5f, 0.05f, 0.05f, 1.f));
                                if (ImGui::Button(T(u8"Remove All Items in Category"), ImVec2(-1, 0))) confirmRemoveCat = true;
                                ImGui::PopStyleColor(3);
                            }
                            else
                            {
                                ImGui::TextColored(ImVec4(1, 0.3f, 0.3f, 1), T(u8"Are you sure? This cannot be undone!"));
                                ImGui::PushStyleColor(ImGuiCol_Button, ImVec4(0.6f, 0.1f, 0.1f, 1.f));
                                ImGui::PushStyleColor(ImGuiCol_ButtonHovered, ImVec4(0.8f, 0.2f, 0.2f, 1.f));
                                ImGui::PushStyleColor(ImGuiCol_ButtonActive, ImVec4(0.5f, 0.05f, 0.05f, 1.f));
                                if (ImGui::Button(T(u8"Confirm Remove"), ImVec2(-1, 0))) { SendCommand(("recovery_remove_category|" + selectedCatId).c_str()); confirmRemoveCat = false; }
                                ImGui::PopStyleColor(3);
                                if (ImGui::Button(T(u8"Cancel"), ImVec2(-1, 0))) confirmRemoveCat = false;
                            }
                        }
                        ImGui::EndTabItem();
                    }
                    ImGui::EndTabBar();
                }
            }
            ImGui::End();

        }
    }

}
