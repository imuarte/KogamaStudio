#include "stdafx.h"
#include "imgui/imgui_internal.h"
#include "Explorer.h"
#include "MenuHelpers.h"
#include "UILocale.h"
#include "Inventory.h"
#include "IconsFontAwesome6.h"
#include "json.hpp"
#include <fstream>
#include <algorithm>
#include <map>
#include <set>

using json = nlohmann::json;
using namespace menu;

namespace Explorer {

    static std::vector<ObjectEntry> objectList;
    static std::map<std::string, std::vector<int>> childrenMap;
    static int rootGroupId = -1;
    static char filterBuf[128] = "";
    static std::string selectedId;
    static std::set<std::string> selectedIds;
    static int lastClickedIdx = -1;
    static bool scrollToSelected = false;
    static std::string renameTargetId;
    static char renameBuf[256] = "";
    static bool renameFocused = false;

    const std::set<std::string>& GetSelectedIds() { return selectedIds; }
    void DeselectAll() { selectedIds.clear(); selectedId.clear(); }

    const ObjectEntry* FindById(const std::string& id)
    {
        for (const auto& obj : objectList)
            if (obj.id == id) return &obj;
        return nullptr;
    }

    static void HandleSelection(int idx)
    {
        ImGuiIO& io = ImGui::GetIO();
        const std::string& id = objectList[idx].id;

        if (io.KeyShift && lastClickedIdx >= 0)
        {
            int lo = lastClickedIdx < idx ? lastClickedIdx : idx;
            int hi = lastClickedIdx > idx ? lastClickedIdx : idx;
            if (!io.KeyCtrl) selectedIds.clear();
            for (int i = lo; i <= hi; i++)
                selectedIds.insert(objectList[i].id);
        }
        else if (io.KeyCtrl)
        {
            if (selectedIds.count(id)) selectedIds.erase(id);
            else                       selectedIds.insert(id);
            lastClickedIdx = idx;
        }
        else
        {
            selectedIds.clear();
            selectedIds.insert(id);
            selectedId     = id;
            lastClickedIdx = idx;
        }
    }

    void SetRootGroupId(int id) { rootGroupId = id; }

    void SetSelected(const std::string& id)
    {
        selectedId = id;
        selectedIds.clear();
        selectedIds.insert(id);
        scrollToSelected = true;
    }

    static void RebuildChildrenMap()
    {
        childrenMap.clear();
        for (int i = 0; i < (int)objectList.size(); ++i)
            childrenMap[std::to_string(objectList[i].groupId)].push_back(i);

        for (auto& kv : childrenMap)
            std::sort(kv.second.begin(), kv.second.end(),
                [](int a, int b) { return objectList[a].order < objectList[b].order; });
    }

    void LoadFromFile(const std::string& path)
    {
        std::ifstream file(path);
        if (!file.is_open()) return;

        auto j = json::parse(file, nullptr, false);
        if (j.is_discarded() || !j.is_array()) return;

        objectList.clear();
        for (auto& item : j)
        {
            int rawId = item.value("id", 0);
            if (rawId == 0) continue;

            ObjectEntry obj;
            obj.id      = std::to_string(rawId);
            obj.order   = item.value("order", 0);
            obj.name    = item.value("name", "");
            obj.type    = item.value("type", "");
            obj.itemId  = item.value("itemId", -1);
            obj.groupId = item.value("groupId", -1);

            if (obj.itemId >= 0)
            {
                const Inventory::InventoryItem* inv = Inventory::FindById(std::to_string(obj.itemId));
                if (inv && !inv->name.empty())
                    obj.name = inv->name;
            }

            objectList.push_back(obj);
        }

        std::sort(objectList.begin(), objectList.end(),
            [](const ObjectEntry& a, const ObjectEntry& b) { return a.order < b.order; });

        RebuildChildrenMap();
    }

    enum class ItemCategory { Pickup, Blueprint, Logic, AdvancedLogic, Special, Unknown };

    static ItemCategory GetCategory(int itemId)
    {
        switch (itemId)
        {
        case 10355: case 10353: case 10360: case 10354: case 10359: case 10356:
        case 7690141: case 239796: case 239800: case 1165835: case 1165838:
        case 12435368: case 46140: case 12730220: case 10352: case 10358: case 1165831:
        case 91197: case 1812739: case 1812737: case 3785736: case 3785733:
        case 3785734: case 3785732: case 8880186: case 8880187: case 12496365:
            return ItemCategory::Pickup;
        case 13481: case 19674: case 20757: case 20756: case 75226: case 75227:
        case 61222: case 12226728: case 349297: case 97157: case 17789:
        case 16408: case 18688: case 9272265: case 6225654:
        case 12305637: case 12324723: case 12339395:
            return ItemCategory::Blueprint;
        case 91196: case 10377: case 10373: case 10374: case 10372: case 2379699:
        case 9016173: case 10375: case 10371: case 10350: case 1165828: case 1165823:
        case 8250560: case 54338: case 10365: case 10380: case 7690139: case 3785730:
        case 10361: case 10362: case 10363: case 10364:
            return ItemCategory::Logic;
        case 4113945: case 5435011: case 10366: case 10368: case 10378:
        case 10367: case 10369: case 9016174: case 10376: case 12532422:
        case 10379: case 4113944: case 10370:
            return ItemCategory::AdvancedLogic;
        case 75579: case 10348:
            return ItemCategory::Special;
        default:
            return ItemCategory::Unknown;
        }
    }

    static const char* TypeIcon(const std::string& type, int itemId)
    {
        switch (GetCategory(itemId))
        {
        case ItemCategory::Pickup:        return ICON_FA_STAR;
        case ItemCategory::Blueprint:     return ICON_FA_WRENCH;
        case ItemCategory::Logic:         return ICON_FA_GEAR;
        case ItemCategory::AdvancedLogic: return ICON_FA_GEARS;
        case ItemCategory::Special:
            return (itemId == 10348) ? ICON_FA_LAYER_GROUP : ICON_FA_CUBE;
        default:
            return (type.find("Group") != std::string::npos) ? ICON_FA_LAYER_GROUP : ICON_FA_CUBE;
        }
    }

    static ImVec4 TypeColor(int itemId)
    {
        switch (GetCategory(itemId))
        {
        case ItemCategory::Pickup:        return ImVec4(1.00f, 0.85f, 0.20f, 1.f);
        case ItemCategory::Blueprint:     return ImVec4(0.40f, 0.85f, 1.00f, 1.f);
        case ItemCategory::Logic:         return ImVec4(1.00f, 0.65f, 0.25f, 1.f);
        case ItemCategory::AdvancedLogic: return ImVec4(0.80f, 0.55f, 1.00f, 1.f);
        case ItemCategory::Special:       return ImVec4(0.55f, 0.90f, 0.65f, 1.f);
        default:                          return ImVec4(0.60f, 0.60f, 0.60f, 1.f);
        }
    }

    static void RenderTree(const std::string& parentId);

    static void RenderItem(int idx)
    {
        ObjectEntry& obj = objectList[idx];
        std::string label   = obj.name.empty() ? obj.type : obj.name;
        std::string display = std::string(TypeIcon(obj.type, obj.itemId)) + " " + label;

        if (renameTargetId == obj.id)
        {
            if (!renameFocused) ImGui::SetKeyboardFocusHere();
            ImGui::SetNextItemWidth(-1);
            ImGui::PushStyleColor(ImGuiCol_FrameBg, ImGui::GetStyleColorVec4(ImGuiCol_Header));
            ImGui::PushStyleColor(ImGuiCol_NavHighlight, ImVec4(0, 0, 0, 0));
            ImGui::PushStyleVar(ImGuiStyleVar_FrameBorderSize, 0.0f);
            bool confirmed = ImGui::InputText(("##rename_" + obj.id).c_str(), renameBuf, sizeof(renameBuf),
                ImGuiInputTextFlags_EnterReturnsTrue | ImGuiInputTextFlags_AutoSelectAll);
            ImGui::PopStyleVar();
            ImGui::PopStyleColor(2);
            if (confirmed)
            {
                obj.name = renameBuf;
                SendCommand((u8"explorer_rename|" + obj.id + u8"|" + renameBuf).c_str());
                renameTargetId.clear();
                renameFocused = false;
            }
            if (!typing) typing = ImGui::IsItemActive();
            if (ImGui::IsItemActive()) renameFocused = true;
            else if (renameFocused) { renameTargetId.clear(); renameFocused = false; }
            return;
        }

        bool isSelected = selectedIds.count(obj.id) > 0;
        ImGui::PushStyleColor(ImGuiCol_Text, TypeColor(obj.itemId));
        bool clicked = ImGui::Selectable((display + u8"##" + obj.id).c_str(), isSelected,
            ImGuiSelectableFlags_AllowOverlap);
        ImGui::PopStyleColor();

        if (clicked)
        {
            HandleSelection(idx);
            if (selectedIds.size() == 1)
            {
                scrollToSelected = false;
                SendCommand((u8"explorer_select|" + obj.id).c_str());
            }
        }

        if (isSelected && scrollToSelected) { ImGui::SetScrollHereY(0.5f); scrollToSelected = false; }
        if (ImGui::IsItemHovered()) ImGui::SetTooltip(u8"ID: %s", obj.id.c_str());

        if (ImGui::BeginDragDropSource(ImGuiDragDropFlags_None))
        {
            bool isMulti = selectedIds.size() > 1 && selectedIds.count(obj.id) > 0;
            if (isMulti)
            {
                std::string payload;
                for (const auto& sid : selectedIds)
                {
                    if (!payload.empty()) payload += ",";
                    payload += sid;
                }
                ImGui::SetDragDropPayload(u8"EXPLORER_ITEMS", payload.c_str(), payload.size() + 1);
                ImGui::Text(u8"%zu objects", selectedIds.size());
            }
            else
            {
                ImGui::SetDragDropPayload(u8"EXPLORER_ITEM", &idx, sizeof(int));
                ImGui::Text(u8"%s", display.c_str());
            }
            ImGui::EndDragDropSource();
        }
        if (ImGui::BeginDragDropTarget())
        {
            {
                ImVec2 itemMin = ImGui::GetItemRectMin();
                ImVec2 itemMax = ImGui::GetItemRectMax();
                bool before = ImGui::GetMousePos().y < (itemMin.y + itemMax.y) * 0.5f;
                float lineY = before ? itemMin.y : itemMax.y;
                ImGui::GetWindowDrawList()->AddLine(
                    ImVec2(itemMin.x, lineY), ImVec2(itemMax.x, lineY),
                    IM_COL32(255, 255, 255, 220), 2.0f);

                if (const ImGuiPayload* payload2 = ImGui::AcceptDragDropPayload(u8"EXPLORER_ITEM",
                    ImGuiDragDropFlags_AcceptNoDrawDefaultRect))
                {
                    int src      = *(const int*)payload2->Data;
                    int insertAt = before ? idx : idx + 1;
                    if (src < insertAt) insertAt--;
                    if (src != insertAt && src >= 0 && src < (int)objectList.size())
                    {
                        ObjectEntry tmp = objectList[src];
                        objectList.erase(objectList.begin() + src);
                        objectList.insert(objectList.begin() + insertAt, tmp);
                        RebuildChildrenMap();

                        std::string order;
                        for (int i = 0; i < (int)objectList.size(); i++)
                        {
                            if (i > 0) order += ",";
                            order += objectList[i].id;
                        }
                        SendCommand((std::string(u8"explorer_set_order|") + order).c_str());
                    }
                }
            }
            ImGui::EndDragDropTarget();
        }


        if (ImGui::BeginPopupContextItem())
        {
            if (ImGui::MenuItem(T(u8"Rename")))
            {
                renameTargetId = obj.id;
                renameFocused  = false;
                strncpy_s(renameBuf, sizeof(renameBuf), label.c_str(), _TRUNCATE);
            }
            ImGui::EndPopup();
        }
    }

    static void RenderGroupNode(int idx)
    {
        ObjectEntry& obj = objectList[idx];
        std::string label   = obj.name.empty() ? obj.type : obj.name;
        std::string display = std::string(TypeIcon(obj.type, obj.itemId)) + " " + label;

        if (renameTargetId == obj.id)
        {
            if (!renameFocused) ImGui::SetKeyboardFocusHere();
            ImGui::SetNextItemWidth(-1);
            ImGui::PushStyleColor(ImGuiCol_FrameBg, ImGui::GetStyleColorVec4(ImGuiCol_Header));
            ImGui::PushStyleColor(ImGuiCol_NavHighlight, ImVec4(0, 0, 0, 0));
            ImGui::PushStyleVar(ImGuiStyleVar_FrameBorderSize, 0.0f);
            bool confirmed = ImGui::InputText(("##rename_" + obj.id).c_str(), renameBuf, sizeof(renameBuf),
                ImGuiInputTextFlags_EnterReturnsTrue | ImGuiInputTextFlags_AutoSelectAll);
            ImGui::PopStyleVar();
            ImGui::PopStyleColor(2);
            if (confirmed)
            {
                obj.name = renameBuf;
                SendCommand((u8"explorer_rename|" + obj.id + u8"|" + renameBuf).c_str());
                renameTargetId.clear();
                renameFocused = false;
            }
            if (!typing) typing = ImGui::IsItemActive();
            if (ImGui::IsItemActive()) renameFocused = true;
            else if (renameFocused) { renameTargetId.clear(); renameFocused = false; }
            return;
        }

        bool isSelected = selectedIds.count(obj.id) > 0;

        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags_OpenOnArrow
            | ImGuiTreeNodeFlags_SpanAvailWidth
            | ImGuiTreeNodeFlags_AllowOverlap;
        if (isSelected) flags |= ImGuiTreeNodeFlags_Selected;

        ImGui::PushStyleColor(ImGuiCol_Text, TypeColor(obj.itemId));
        bool open = ImGui::TreeNodeEx((display + u8"##" + obj.id).c_str(), flags);
        ImGui::PopStyleColor();

        if (ImGui::IsItemClicked() && !ImGui::IsItemToggledOpen())
        {
            HandleSelection(idx);
            if (selectedIds.size() == 1)
            {
                scrollToSelected = false;
                SendCommand((u8"explorer_select|" + obj.id).c_str());
            }
        }
        if (isSelected && scrollToSelected) { ImGui::SetScrollHereY(0.5f); scrollToSelected = false; }
        if (ImGui::IsItemHovered()) ImGui::SetTooltip(u8"ID: %s", obj.id.c_str());

        if (ImGui::BeginDragDropSource(ImGuiDragDropFlags_None))
        {
            bool isMulti = selectedIds.size() > 1 && selectedIds.count(obj.id) > 0;
            if (isMulti)
            {
                std::string payload;
                for (const auto& sid : selectedIds) { if (!payload.empty()) payload += ","; payload += sid; }
                ImGui::SetDragDropPayload(u8"EXPLORER_ITEMS", payload.c_str(), payload.size() + 1);
                ImGui::Text(u8"%zu objects", selectedIds.size());
            }
            else
            {
                ImGui::SetDragDropPayload(u8"EXPLORER_ITEM", &idx, sizeof(int));
                ImGui::Text(u8"%s", display.c_str());
            }
            ImGui::EndDragDropSource();
        }

        if (obj.type.find("Group") != std::string::npos && ImGui::BeginDragDropTarget())
        {
            if (const ImGuiPayload* payload = ImGui::AcceptDragDropPayload(u8"EXPLORER_ITEMS"))
            {
                SendCommand((std::string(u8"explorer_move_to_group|") + obj.id + u8"|" + (const char*)payload->Data).c_str());
            }
            else if (const ImGuiPayload* payload = ImGui::AcceptDragDropPayload(u8"EXPLORER_ITEM"))
            {
                int src = *(const int*)payload->Data;
                SendCommand((std::string(u8"explorer_move_to_group|") + obj.id + u8"|" + objectList[src].id).c_str());
            }
            else if (const ImGuiPayload* payload = ImGui::AcceptDragDropPayload(u8"INVENTORY_ITEM"))
            {
                SendCommand((std::string(u8"world_place_item_in_group|") + (const char*)payload->Data + u8"|" + obj.id).c_str());
            }
            ImGui::EndDragDropTarget();
        }

        if (ImGui::BeginPopupContextItem())
        {
            if (ImGui::MenuItem(T(u8"Rename")))
            {
                renameTargetId = obj.id;
                renameFocused  = false;
                strncpy_s(renameBuf, sizeof(renameBuf), label.c_str(), _TRUNCATE);
            }
            ImGui::EndPopup();
        }

        if (open)
        {
            RenderTree(obj.id);
            ImGui::TreePop();
        }
    }

    static void RenderTree(const std::string& parentId)
    {
        auto it = childrenMap.find(parentId);
        if (it == childrenMap.end()) return;
        for (int idx : it->second)
        {
            bool isGroup = objectList[idx].type.find("Group") != std::string::npos;
            if (childrenMap.count(objectList[idx].id) || isGroup)
                RenderGroupNode(idx);
            else
                RenderItem(idx);
        }
    }

    void Render()
    {
        if (!ImGui::Begin((std::string(T(u8"Explorer")) + u8"###Explorer").c_str(), nullptr, ImGuiWindowFlags_NoCollapse))
        {
            ImGui::End();
            return;
        }

        if (ImGui::Button(T(u8"Refresh"))) SendCommand(u8"explorer_refresh");
        ImGui::SameLine();
        if (ImGui::Button(T(u8"New Group"))) SendCommand(u8"explorer_create_group");
        ImGui::SameLine();
        ImGui::SetNextItemWidth(-1);
        ImGui::InputText(u8"##explorer_filter", filterBuf, sizeof(filterBuf));
        if (!typing) typing = ImGui::IsItemActive();

        ImGui::Separator();
        ImGui::TextDisabled("%s: %zu", T(u8"Objects"), objectList.size());

        ImGui::BeginChild(u8"##explorer_list", ImVec2(0, 0), false);

        if (objectList.empty())
        {
            ImGui::TextDisabled(T(u8"No objects"));
        }
        else
        {
            std::string filter = filterBuf;
            std::transform(filter.begin(), filter.end(), filter.begin(), ::tolower);

            if (!filter.empty())
            {
                for (int i = 0; i < (int)objectList.size(); ++i)
                {
                    ObjectEntry& obj = objectList[i];
                    std::string label = obj.name.empty() ? obj.type : obj.name;
                    std::string lower = label;
                    std::transform(lower.begin(), lower.end(), lower.begin(), ::tolower);
                    if (lower.find(filter) == std::string::npos && obj.id.find(filter) == std::string::npos)
                        continue;
                    RenderItem(i);
                }
            }
            else if (rootGroupId >= 0)
            {
                RenderTree(std::to_string(rootGroupId));
            }
            else
            {
                for (int i = 0; i < (int)objectList.size(); ++i)
                    RenderItem(i);
            }
        }

        if (ImGui::BeginDragDropTargetCustom(ImGui::GetCurrentWindow()->Rect(), ImGui::GetID(u8"##explorer_list")))
        {
            if (const ImGuiPayload* payload = ImGui::AcceptDragDropPayload(u8"INVENTORY_ITEM"))
            {
                SendCommand((std::string(u8"world_place_item|") + (const char*)payload->Data).c_str());
            }
            else if (rootGroupId >= 0)
            {
                std::string root = std::to_string(rootGroupId);
                if (const ImGuiPayload* payload = ImGui::AcceptDragDropPayload(u8"EXPLORER_ITEMS"))
                    SendCommand((std::string(u8"explorer_move_to_group|") + root + u8"|" + (const char*)payload->Data).c_str());
                else if (const ImGuiPayload* payload = ImGui::AcceptDragDropPayload(u8"EXPLORER_ITEM"))
                    SendCommand((std::string(u8"explorer_move_to_group|") + root + u8"|" + objectList[*(const int*)payload->Data].id).c_str());
            }
            ImGui::EndDragDropTarget();
        }
        ImGui::EndChild();
        ImGui::End();
    }
}
