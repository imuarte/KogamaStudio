#include <stdafx.h>
#include <windows.h>
#include <thread>
#include <string>
#include "pipe.h"
#include "Properties.h"
#include "Clipboard.h"
#include "Recovery.h"
#include "Console.h"
#include "Players.h"
#include "Explorer.h"
#include "GameInfo.h"
#include "Inventory.h"


namespace pipe {

    bool cursorVisible = false;
    bool gameInitialized = false;
    bool openMenu = false;
    float generateProgress = 0.0f;

    void ProcessCommand(const std::string& cmd)
    {
        size_t pos = cmd.find("|");
        std::string command = cmd.substr(0, pos);
        std::string param = (pos != std::string::npos) ? cmd.substr(pos + 1) : "";

        DebugLog("[pipe] Command: %s, Param: %s\n", command.c_str(), param.c_str());

        ImGuiIO& io = ImGui::GetIO();

        if (command == "cursor")
        {
            if (param == "true")
            {
                cursorVisible = true;
            }
            else if (param == "false")
            {
                cursorVisible = false;
            }
        }
        else if (command == "game_initialized")
        {
            menu::isOpen = true;
            gameInitialized = true;
        }
        else if (command == "key_down")
        {
            if (param == "F2")
            {
                openMenu = !openMenu;
            }
        }
        else if (command == "generate_progress")
        {
            float progress = std::stof(param);
            generateProgress = progress;
        }
        else if (command == "properties_position_x")
        {
            Properties::positionX = std::stof(param);
        }
        else if (command == "properties_position_y")
        {
            Properties::positionY = std::stof(param);
        }
        else if (command == "properties_position_z")
        {
            Properties::positionZ = std::stof(param);
        }
        else if (command == "properties_rotation_x")
        {
            Properties::rotationX = std::stof(param);
        }
        else if (command == "properties_rotation_y")
        {
            Properties::rotationY = std::stof(param);
        }
        else if (command == "properties_rotation_z")
        {
            Properties::rotationZ = std::stof(param);
        }
        else if (command == "properties_object_id")
        {
            Properties::targetObjectId = param;
        }
        else if (command == "properties_is_model")
        {
            if (param == "true")
            {
                Properties::isModel = true;
            }
            else if (param == "false")
            {
                Properties::isModel = false;
            }
        }
        else if (command == "clipboard_preview_paste_model")
        {
            if (param == "true")
            {
                Properties::isPrevievPaste = true;
            }
            else if (param == "false")
            {
                Properties::isPrevievPaste = false;
            }
        }
        else if (command == "recovery_mode")
        {
            Recovery::recoveryOpen = true;
        }
        else if (command == "recovery_corruption_detected")
        {
            Recovery::corruptionDetected = true;
            Recovery::recoveryOpen = true;
        }
        else if (command == "recovery_problem")
        {
            size_t p1 = param.find("|");
            std::string objectId = param.substr(0, p1);
            std::string rest = (p1 != std::string::npos) ? param.substr(p1 + 1) : "";
            size_t p2 = rest.find("|");
            std::string actionType = rest.substr(0, p2);
            std::string description = (p2 != std::string::npos) ? rest.substr(p2 + 1) : rest;
            Recovery::AddProblem(objectId, actionType, description);
        }
        else if (command == "console_log")
        {
            AppLog::AddLog(AppLog::LogType::Incoming, param.c_str());
        }
        else if (command == "console_warning")
        {
            AppLog::AddLog(AppLog::LogType::Warning, param.c_str());
        }
        else if (command == "console_error")
        {
            AppLog::AddLog(AppLog::LogType::Error, param.c_str());
        }
        else if (command == "players_list")
        {
            Players::SetPlayers(param);
        }
        else if (command == "explorer_ready")
        {
            Explorer::LoadFromFile(param);
        }
        else if (command == "explorer_set_selected")
        {
            Explorer::SetSelected(param);
        }
        else if (command == "gameinfo_update")
        {
            GameInfo::SetData(param);
        }
        else if (command == "inventory_item")
        {
            // Format: id|name|typeId|categoryId|categoryName|resellable|priceGold|purchased|authorProfileId|slotPosition|description
            auto trim = [](std::string s) -> std::string { while (!s.empty() && (s.back() == '\r' || s.back() == '\n' || s.back() == ' ')) s.pop_back(); return s; };
            auto nextField = [&](std::string& s) -> std::string {
                size_t p = s.find('|');
                if (p == std::string::npos) { std::string v = trim(s); s.clear(); return v; }
                std::string v = trim(s.substr(0, p));
                s = s.substr(p + 1);
                return v;
            };

            Inventory::InventoryItem item;
            std::string rest = param;
            item.id             = nextField(rest);
            item.name           = nextField(rest);
            item.typeId         = nextField(rest);
            item.categoryId     = nextField(rest);
            item.categoryName   = nextField(rest);
            item.resellable     = (nextField(rest) == "True");
            { std::string f = nextField(rest); item.priceGold    = f.empty() ? 0 : ::atoi(f.c_str()); }
            item.purchased      = (nextField(rest) == "True");
            item.authorProfileId = nextField(rest);
            { std::string f = nextField(rest); item.slotPosition = f.empty() ? 0 : ::atoi(f.c_str()); }
            item.description    = rest;

            Inventory::AddItem(item);
        }
    }

    void ListenForCommands()
    {
        std::thread([]() {
            while (true) {
                HANDLE hPipe = CreateNamedPipeA(
                    "\\\\.\\pipe\\KogamaStudio_CSharp",
                    PIPE_ACCESS_INBOUND,
                    PIPE_TYPE_BYTE,
                    1, 0, 0, 0, NULL
                );

                if (hPipe == INVALID_HANDLE_VALUE) continue;

                if (ConnectNamedPipe(hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED) {
                    char buffer[65536] = { 0 };
                    DWORD bytesRead = 0;

                    if (ReadFile(hPipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL))
                    {
                        buffer[bytesRead] = '\0';
                        ProcessCommand(std::string(buffer));
                    }
                }
                CloseHandle(hPipe);
            }
            }).detach();
    }
}