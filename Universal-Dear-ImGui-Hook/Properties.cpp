#include "Properties.h"
#include "MenuHelpers.h"
#include "pipe.h"
#include "Inventory.h"
#include "History.h"

using namespace menu;

namespace Properties {

    std::string targetObjectId = u8"-1";
    float positionX = 0.0f;
    float positionY = 0.0f;
    float positionZ = 0.0f;
    float rotationX = 0.0f;
    float rotationY = 0.0f;
    float rotationZ = 0.0f;
    bool isModel = false;
    bool isPrevievPaste = false;

    static std::string sPrevObjectId = "";
    static std::string sPrevItemId = "";

	void Render() {
        ImGui::SetNextWindowSize(ImVec2(300, 400), ImGuiCond_FirstUseEver);
        ImGui::SetNextWindowPos(ImVec2(490, 20), ImGuiCond_FirstUseEver);
        ImGuiWindowFlags newFlags = ImGuiWindowFlags_NoCollapse;
        if (ImGui::Begin((std::string(T(u8"Properties")) + u8"###Properties").c_str(), nullptr, newFlags))
        {
            bool hasObject = targetObjectId != u8"-1";
            bool hasItem = !hasObject && Inventory::HasSelected();

            std::string currentItemId = hasItem ? Inventory::GetSelected().id : "";
            bool selectionChanged = (targetObjectId != sPrevObjectId) || (currentItemId != sPrevItemId);
            if (selectionChanged)
            {
                sPrevObjectId = targetObjectId;
                sPrevItemId = currentItemId;
                ImGui::SetScrollHereY(0.0f);
            }

            if (!hasObject && !hasItem)
            {
                ImGui::TextDisabled(T(u8"Nothing selected"));
            }

            if (hasObject)
            {
                if (selectionChanged) ImGui::SetNextItemOpen(true, ImGuiCond_Always);
                if (ImGui::CollapsingHeader(T(u8"Object")))
                {
                    ImGui::Text(u8"%s: %s", T(u8"Object ID"), targetObjectId.c_str());

                    if (ImGui::Button(T(u8"Remove"))) SendCommand(u8"properties_remove");
                    ImGui::SameLine();
                    if (ImGui::Button(T(u8"Teleport to"))) SendCommand((u8"recovery_teleport_to_object|" + targetObjectId).c_str());
                    ImGui::SameLine();
                    if (ImGui::Button(T(u8"Add to Inventory"))) SendCommand((u8"inventory_add_object|" + targetObjectId).c_str());
                }

                if (selectionChanged) ImGui::SetNextItemOpen(true, ImGuiCond_Always);
                if (ImGui::CollapsingHeader(T(u8"Transform")))
                {
                    ImGui::Text(T(u8"Position"));
                    menu::DragFloatInput(u8"X##pos_x", positionX, u8"properties_position_x", typing, 0.1f);
                    bool posXDone = ImGui::IsItemDeactivatedAfterEdit();
                    menu::DragFloatInput(u8"Y##pos_y", positionY, u8"properties_position_y", typing, 0.1f);
                    bool posYDone = ImGui::IsItemDeactivatedAfterEdit();
                    menu::DragFloatInput(u8"Z##pos_z", positionZ, u8"properties_position_z", typing, 0.1f);
                    bool posZDone = ImGui::IsItemDeactivatedAfterEdit();

                    ImGui::Text(T(u8"Rotation"));
                    menu::DragFloatInput(u8"X##rot_x", rotationX, u8"properties_rotation_x", typing, 1.0f);
                    bool rotXDone = ImGui::IsItemDeactivatedAfterEdit();
                    menu::DragFloatInput(u8"Y##rot_y", rotationY, u8"properties_rotation_y", typing, 1.0f);
                    bool rotYDone = ImGui::IsItemDeactivatedAfterEdit();
                    menu::DragFloatInput(u8"Z##rot_z", rotationZ, u8"properties_rotation_z", typing, 1.0f);
                    bool rotZDone = ImGui::IsItemDeactivatedAfterEdit();

                    if (posXDone || posYDone || posZDone || rotXDone || rotYDone || rotZDone)
                    {
                        char buf[256];
                        snprintf(buf, sizeof(buf),
                            "Transform #%s pos(%.2f, %.2f, %.2f) rot(%.1f, %.1f, %.1f)",
                            targetObjectId.c_str(),
                            positionX, positionY, positionZ,
                            rotationX, rotationY, rotationZ);
                        History::Add(buf);
                    }
                }

                if (isModel)
                {
                    if (selectionChanged) ImGui::SetNextItemOpen(true, ImGuiCond_Always);
                    if (ImGui::CollapsingHeader(T(u8"Model")))
                    {
                        bool building = pipe::isPasteBuilding;

                        if (ImGui::Button(T(u8"Copy Cubes")))
                            SendCommand(u8"clipboard_copy_model");

                        if (building) ImGui::BeginDisabled();
                        if (ImGui::Button(T(u8"Paste Cubes")))
                            SendCommand(u8"clipboard_paste_model");
                        if (building) ImGui::EndDisabled();

                        if (!building)
                        {
                            if (ImGui::Checkbox(T(u8"Preview Paste Cubes"), &isPrevievPaste))
                            {
                                if (isPrevievPaste) SendCommand(u8"clipboard_preview_paste_model|true");
                                else SendCommand(u8"clipboard_preview_paste_model|false");
                            }
                        }

                        if (building)
                        {
                            ImGui::Spacing();
                            ImGui::Separator();

                            float progress = pipe::pasteTotalCubes > 0
                                ? (float)pipe::pastePlacedCubes / (float)pipe::pasteTotalCubes
                                : 0.0f;

                            char overlay[64];
                            snprintf(overlay, sizeof(overlay), "%d / %d  (%.0f%%)",
                                pipe::pastePlacedCubes, pipe::pasteTotalCubes, progress * 100.0f);
                            ImGui::ProgressBar(progress, ImVec2(-1, 0), overlay);

                            if (pipe::pasteEtaSeconds >= 0.0f)
                            {
                                int eta = (int)pipe::pasteEtaSeconds;
                                char etaBuf[32];
                                if (eta >= 60)
                                    snprintf(etaBuf, sizeof(etaBuf), "%dm %ds", eta / 60, eta % 60);
                                else
                                    snprintf(etaBuf, sizeof(etaBuf), "%ds", eta);
                                ImGui::TextDisabled(T(u8"Time left: %s"), etaBuf);
                                ImGui::SameLine();
                            }

                            ImGui::PushStyleColor(ImGuiCol_Button,        ImVec4(0.6f, 0.1f, 0.1f, 1.0f));
                            ImGui::PushStyleColor(ImGuiCol_ButtonHovered, ImVec4(0.8f, 0.2f, 0.2f, 1.0f));
                            ImGui::PushStyleColor(ImGuiCol_ButtonActive,  ImVec4(0.5f, 0.05f, 0.05f, 1.0f));
                            if (ImGui::Button(T(u8"Cancel")))
                                SendCommand(u8"generate_cancel");
                            ImGui::PopStyleColor(3);
                        }
                    }
                }
            }

            if (hasItem)
            {
                if (selectionChanged) ImGui::SetNextItemOpen(true, ImGuiCond_Always);
                if (ImGui::CollapsingHeader(T(u8"Item")))
                {
                    const Inventory::InventoryItem& item = Inventory::GetSelected();

                    ImGui::PushFont(ImGui::GetIO().Fonts->Fonts.Size > 1 ? ImGui::GetIO().Fonts->Fonts[1] : ImGui::GetIO().Fonts->Fonts[0]);
                    std::string displayName = item.name.empty() ? (u8"[ID " + item.id + u8"]") : item.name;
                    ImGui::TextUnformatted(displayName.c_str());
                    ImGui::PopFont();

                    ImGui::Separator();

                    if (ImGui::BeginTable(u8"##item_props", 2, ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_SizingStretchProp))
                    {
                        ImGui::TableSetupColumn(u8"##key",   ImGuiTableColumnFlags_WidthStretch, 0.4f);
                        ImGui::TableSetupColumn(u8"##value", ImGuiTableColumnFlags_WidthStretch, 0.6f);

                        auto Row = [](const char* key, const std::string& value) {
                            ImGui::TableNextRow();
                            ImGui::TableSetColumnIndex(0); ImGui::TextDisabled("%s", key);
                            ImGui::TableSetColumnIndex(1); ImGui::TextUnformatted(value.c_str());
                        };

                        Row(T(u8"Item ID"),     item.id);
                        Row(T(u8"Type ID"),     item.typeId);
                        Row(T(u8"Category"),    item.categoryName);
                        Row(T(u8"Category ID"), item.categoryId);
                        Row(T(u8"Slot"),        std::to_string(item.slotPosition));
                        Row(T(u8"Resellable"),  item.resellable ? T(u8"Yes") : T(u8"No"));
                        Row(T(u8"Price"),       std::to_string(item.priceGold));
                        Row(T(u8"Purchased"),   item.purchased ? T(u8"Yes") : T(u8"No"));
                        Row(T(u8"Author ID"),   item.authorProfileId);

                        ImGui::EndTable();
                    }

                    if (!item.description.empty())
                    {
                        ImGui::Spacing();
                        ImGui::TextDisabled(T(u8"Description"));
                        ImGui::Separator();
                        ImGui::TextWrapped("%s", item.description.c_str());
                    }

                    ImGui::Spacing();
                    ImGui::Separator();

                    ImGui::PushStyleColor(ImGuiCol_Button,        ImVec4(0.6f, 0.1f, 0.1f, 1.0f));
                    ImGui::PushStyleColor(ImGuiCol_ButtonHovered, ImVec4(0.8f, 0.2f, 0.2f, 1.0f));
                    ImGui::PushStyleColor(ImGuiCol_ButtonActive,  ImVec4(0.5f, 0.05f, 0.05f, 1.0f));
                    if (ImGui::Button(T(u8"Remove from Inventory"), ImVec2(-1, 0)))
                        SendCommand((u8"inventory_remove|" + item.id).c_str());
                    ImGui::PopStyleColor(3);
                }
            }
        }
        ImGui::End();
	}
}
