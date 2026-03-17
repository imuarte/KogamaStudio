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

    enum class SortMode { Custom = 0, Alpha = 1, Distance = 2 };
    static SortMode sortMode = SortMode::Custom;
    static bool     sortDesc = false;
    static float    camX = 0.f, camY = 0.f, camZ = 0.f;
    static float    addCubeScale = 1.0f;

    // --- Creation modal state ---
    static bool  createModalOpen  = false;
    static int   createType       = 0;   // 0=Group, 1=CubeModel
    static int   createParentId   = -1;
    static float createPos[3]     = {0.f, 10.f, 0.f};
    static float createRot[3]     = {0.f, 0.f, 0.f};
    static float createScale[3]   = {1.f, 1.f, 1.f};
    static float createCubeScale2 = 1.f;
    static int   createMaterialId = 0;
    static bool  createLocalOwner = false;

    // Locale-safe float formatter
    static std::string FF(float v)
    {
        char buf[32]; snprintf(buf, sizeof(buf), "%.6g", v);
        for (char* p = buf; *p; ++p) if (*p == ',') *p = '.';
        return buf;
    }

    static void OpenCreateModal(int type, int parentId)
    {
        createType     = type;
        createParentId = parentId;
        createPos[0]   = 0.f; createPos[1] = 10.f; createPos[2] = 0.f;
        createRot[0]   = createRot[1] = createRot[2] = 0.f;
        createScale[0] = createScale[1] = createScale[2] = 1.f;
        createCubeScale2  = 1.f;
        createMaterialId  = 0;
        createLocalOwner  = false;
        createModalOpen   = true;
    }

    static void RenderCreateModal()
    {
        if (!createModalOpen) return;
        ImGui::SetNextWindowSize(ImVec2(340, 0), ImGuiCond_Always);
        ImGui::SetNextWindowPos(ImGui::GetMainViewport()->GetCenter(), ImGuiCond_Always, ImVec2(0.5f, 0.5f));
        bool open = true;
        ImGuiWindowFlags flags = ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoMove | ImGuiWindowFlags_NoDocking;
        const char* title = createType == 0 ? (ICON_FA_LAYER_GROUP u8" New Group###create_modal")
                                            : (ICON_FA_CUBE        u8" New CubeModel###create_modal");
        if (!ImGui::Begin(title, &open, flags))
        {
            ImGui::End();
            if (!open) createModalOpen = false;
            return;
        }

        // Parent group (Groups only — CubeModels are always created in root group)
        if (createType == 0)
        {
            ImGui::Text(T(u8"Parent Group ID"));
            ImGui::SameLine();
            ImGui::SetNextItemWidth(90.f);
            ImGui::InputInt(u8"##cpg", &createParentId);
            if (createParentId >= 0)
            {
                const ObjectEntry* pg = FindById(std::to_string(createParentId));
                if (pg) { ImGui::SameLine(); ImGui::TextDisabled("(%s)", pg->name.c_str()); }
            }
            ImGui::Separator();
        }

        ImGui::DragFloat3(T(u8"Position"),          createPos,   0.5f);
        ImGui::DragFloat3(T(u8"Rotation (euler)"),  createRot,   1.f,  -360.f, 360.f);

        if (createType == 0)
        {
            ImGui::DragFloat3(T(u8"Scale"), createScale, 0.01f, 0.01f, 100.f);
            ImGui::Checkbox(T(u8"Local owner"), &createLocalOwner);
        }
        else
        {
            ImGui::DragFloat(T(u8"Scale##cs"), &createCubeScale2, 0.01f, 0.1f, 50.f);
            ImGui::InputInt(T(u8"Material ID"), &createMaterialId);
            if (createMaterialId < 0) createMaterialId = 0;
            ImGui::TextDisabled(T(u8"(placed in root group, owned by server)"));
        }

        ImGui::Separator();
        bool confirm = ImGui::Button(T(u8"Create"), ImVec2(150.f, 0));
        ImGui::SameLine();
        bool cancel  = ImGui::Button(T(u8"Cancel"), ImVec2(150.f, 0));

        if (confirm)
        {
            char cmd[512];
            int lo = createLocalOwner ? 1 : 0;
            if (createType == 0)
            {
                snprintf(cmd, sizeof(cmd), "explorer_create_group|%d|%s|%s|%s|%s|%s|%s|%s|%s|%s|%d",
                    createParentId,
                    FF(createPos[0]).c_str(), FF(createPos[1]).c_str(), FF(createPos[2]).c_str(),
                    FF(createRot[0]).c_str(), FF(createRot[1]).c_str(), FF(createRot[2]).c_str(),
                    FF(createScale[0]).c_str(), FF(createScale[1]).c_str(), FF(createScale[2]).c_str(), lo);
            }
            else
            {
                snprintf(cmd, sizeof(cmd), "explorer_create_cubemodel|%s|%s|%s|%s|%s|%s|%s|%d",
                    FF(createPos[0]).c_str(), FF(createPos[1]).c_str(), FF(createPos[2]).c_str(),
                    FF(createRot[0]).c_str(), FF(createRot[1]).c_str(), FF(createRot[2]).c_str(),
                    FF(createCubeScale2).c_str(), createMaterialId);
            }
            SendCommand(cmd);
            createModalOpen = false;
        }
        if (cancel) createModalOpen = false;

        ImGui::End();
    }

    void SetCameraPos(float x, float y, float z) { camX = x; camY = y; camZ = z; }

    static float DistSq(const ObjectEntry& e)
    {
        float dx = e.x - camX, dy = e.y - camY, dz = e.z - camZ;
        return dx * dx + dy * dy + dz * dz;
    }

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
            obj.x       = item.value("x", 0.f);
            obj.y       = item.value("y", 0.f);
            obj.z       = item.value("z", 0.f);

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
            ImGui::Separator();
            if (ImGui::MenuItem(ICON_FA_LAYER_GROUP u8"  " T(u8"Add Group here")))
                { OpenCreateModal(0, obj.groupId); ImGui::CloseCurrentPopup(); }
            if (ImGui::MenuItem(ICON_FA_CUBE u8"  " T(u8"Add CubeModel here")))
                { OpenCreateModal(1, obj.groupId); ImGui::CloseCurrentPopup(); }
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
            ImGui::Separator();
            int gid = std::stoi(obj.id);
            if (ImGui::MenuItem(ICON_FA_LAYER_GROUP u8"  " T(u8"Add Group inside")))
                { OpenCreateModal(0, gid); ImGui::CloseCurrentPopup(); }
            if (ImGui::MenuItem(ICON_FA_LAYER_GROUP u8"  " T(u8"Add Group here")))
                { OpenCreateModal(0, obj.groupId); ImGui::CloseCurrentPopup(); }
            if (ImGui::MenuItem(ICON_FA_CUBE u8"  " T(u8"Add CubeModel")))
                { OpenCreateModal(1, gid); ImGui::CloseCurrentPopup(); }
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
        if (ImGui::Button(u8"+" u8"##add_btn"))
            ImGui::OpenPopup(u8"##add_type_popup");
        if (ImGui::IsItemHovered()) ImGui::SetTooltip(T(u8"Add object"));
        if (ImGui::BeginPopup(u8"##add_type_popup"))
        {
            int targetId = rootGroupId;
            if (!selectedId.empty())
            {
                const ObjectEntry* sel = FindById(selectedId);
                if (sel && sel->type.find("Group") != std::string::npos)
                    targetId = std::stoi(selectedId);
                else if (sel)
                    targetId = sel->groupId;
            }
            if (ImGui::MenuItem(ICON_FA_LAYER_GROUP u8"  Group"))
                { OpenCreateModal(0, targetId); ImGui::CloseCurrentPopup(); }
            if (ImGui::MenuItem(ICON_FA_CUBE u8"  CubeModel"))
                { OpenCreateModal(1, targetId); ImGui::CloseCurrentPopup(); }
            ImGui::EndPopup();
        }
        ImGui::SameLine();
        if (ImGui::Button(ICON_FA_FILTER))
            ImGui::OpenPopup(u8"##sort_popup");
        if (ImGui::IsItemHovered()) ImGui::SetTooltip(T(u8"Sort"));
        if (ImGui::BeginPopup(u8"##sort_popup"))
        {
            ImGui::TextDisabled(T(u8"Sort by"));
            ImGui::Separator();
            if (ImGui::RadioButton(T(u8"Custom order"), sortMode == SortMode::Custom))
                sortMode = SortMode::Custom;
            if (ImGui::RadioButton(T(u8"Alphabetical"), sortMode == SortMode::Alpha))
                sortMode = SortMode::Alpha;
            if (ImGui::RadioButton(T(u8"Distance from camera"), sortMode == SortMode::Distance))
                sortMode = SortMode::Distance;
            ImGui::Separator();
            bool asc = !sortDesc;
            if (ImGui::RadioButton(T(u8"Ascending"),  asc))  sortDesc = false;
            if (ImGui::RadioButton(T(u8"Descending"), sortDesc)) sortDesc = true;
            ImGui::EndPopup();
        }
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

            bool useFlat = !filter.empty() || sortMode != SortMode::Custom || sortDesc;
            if (useFlat)
            {
                // Build index list, apply filter
                std::vector<int> indices;
                indices.reserve(objectList.size());
                for (int i = 0; i < (int)objectList.size(); ++i)
                {
                    if (!filter.empty())
                    {
                        const ObjectEntry& obj = objectList[i];
                        std::string label = obj.name.empty() ? obj.type : obj.name;
                        std::transform(label.begin(), label.end(), label.begin(), ::tolower);
                        if (label.find(filter) == std::string::npos && obj.id.find(filter) == std::string::npos)
                            continue;
                    }
                    indices.push_back(i);
                }

                // Sort by chosen mode (custom = already in order, no re-sort needed)
                if (sortMode == SortMode::Alpha)
                {
                    std::sort(indices.begin(), indices.end(), [](int a, int b) {
                        const std::string& na = objectList[a].name.empty() ? objectList[a].type : objectList[a].name;
                        const std::string& nb = objectList[b].name.empty() ? objectList[b].type : objectList[b].name;
                        std::string la = na, lb = nb;
                        std::transform(la.begin(), la.end(), la.begin(), ::tolower);
                        std::transform(lb.begin(), lb.end(), lb.begin(), ::tolower);
                        return la < lb;
                    });
                }
                else if (sortMode == SortMode::Distance)
                {
                    std::sort(indices.begin(), indices.end(), [](int a, int b) {
                        return DistSq(objectList[a]) < DistSq(objectList[b]);
                    });
                }

                if (sortDesc) std::reverse(indices.begin(), indices.end());

                for (int i : indices)
                    RenderItem(i);
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

        RenderCreateModal();
    }
}
