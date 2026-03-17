#pragma once
#include <string>
#include <vector>
#include <set>

namespace Explorer {
    struct ObjectEntry {
        std::string id;
        std::string type;
        std::string name;
        int itemId  = -1;
        int order   = 0;
        int groupId = -1;
    };

    void LoadFromFile(const std::string& path);
    void SetSelected(const std::string& id);
    void SetRootGroupId(int id);
    void Render();
    const std::set<std::string>& GetSelectedIds();
    const ObjectEntry* FindById(const std::string& id);
    void DeselectAll();
}
