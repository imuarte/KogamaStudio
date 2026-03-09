#pragma once
// Font Awesome 6 Free - Solid icons (UTF-8 encoded)
// Font file: fa-solid-900.ttf  (in %LOCALAPPDATA%\KogamaStudio\Fonts\)
// Codepoints are identical in FA6 and FA7.

#define ICON_MIN_FA 0xf000
#define ICON_MAX_FA 0xf8ff
#define FONT_ICON_FILE_NAME_FAS "fa-solid-900.ttf"

// Helper: icons load into the main font via MergeMode so no font switching needed.

// ---- Navigation / Arrows ----
#define ICON_FA_ARROW_LEFT              u8"\xef\x81\xa0"   // f060
#define ICON_FA_ARROW_RIGHT             u8"\xef\x81\xa1"   // f061
#define ICON_FA_ARROW_UP                u8"\xef\x81\xa2"   // f062
#define ICON_FA_ARROW_DOWN              u8"\xef\x81\xa3"   // f063
#define ICON_FA_EXPAND                  u8"\xef\x81\xa5"   // f065
#define ICON_FA_COMPRESS                u8"\xef\x81\xa6"   // f066
#define ICON_FA_ARROWS_UP_DOWN_LEFT_RIGHT u8"\xef\x81\x87" // f047

// ---- Common actions ----
#define ICON_FA_MAGNIFYING_GLASS        u8"\xef\x80\x82"   // f002
#define ICON_FA_SEARCH                  ICON_FA_MAGNIFYING_GLASS
#define ICON_FA_CHECK                   u8"\xef\x80\x8c"   // f00c
#define ICON_FA_XMARK                   u8"\xef\x80\x8d"   // f00d
#define ICON_FA_TIMES                   ICON_FA_XMARK
#define ICON_FA_PLUS                    u8"\xef\x81\xa7"   // f067
#define ICON_FA_MINUS                   u8"\xef\x81\xa8"   // f068
#define ICON_FA_ROTATE                  u8"\xef\x80\xa1"   // f021
#define ICON_FA_ROTATE_LEFT             u8"\xef\x8b\xaa"   // f2ea
#define ICON_FA_ROTATE_RIGHT            u8"\xef\x8b\xb9"   // f2f9
#define ICON_FA_COPY                    u8"\xef\x83\x85"   // f0c5
#define ICON_FA_PASTE                   u8"\xef\x83\xaa"   // f0ea
#define ICON_FA_FLOPPY_DISK             u8"\xef\x83\x87"   // f0c7
#define ICON_FA_SAVE                    ICON_FA_FLOPPY_DISK
#define ICON_FA_TRASH_CAN               u8"\xef\x8b\xad"   // f2ed
#define ICON_FA_TRASH                   u8"\xef\x87\xb8"   // f1f8
#define ICON_FA_PEN_TO_SQUARE           u8"\xef\x81\x84"   // f044
#define ICON_FA_EDIT                    ICON_FA_PEN_TO_SQUARE
#define ICON_FA_FILTER                  u8"\xef\x82\xb0"   // f0b0
#define ICON_FA_LINK                    u8"\xef\x83\x81"   // f0c1
#define ICON_FA_LINK_SLASH              u8"\xef\x84\xa7"   // f127
#define ICON_FA_BAN                     u8"\xef\x81\x9e"   // f05e

// ---- Status / Feedback ----
#define ICON_FA_CIRCLE_INFO             u8"\xef\x81\x9a"   // f05a
#define ICON_FA_INFO                    u8"\xef\x84\xa9"   // f129
#define ICON_FA_TRIANGLE_EXCLAMATION    u8"\xef\x81\xb1"   // f071
#define ICON_FA_CIRCLE_CHECK            u8"\xef\x81\x98"   // f058
#define ICON_FA_CIRCLE_XMARK            u8"\xef\x81\x97"   // f057
#define ICON_FA_BOLT                    u8"\xef\x83\xa7"   // f0e7
#define ICON_FA_FLAG                    u8"\xef\x80\xa4"   // f024
#define ICON_FA_STAR                    u8"\xef\x80\x85"   // f005

// ---- Objects / Files ----
#define ICON_FA_CUBE                    u8"\xef\x86\xb2"   // f1b2
#define ICON_FA_CUBES                   u8"\xef\x86\xb3"   // f1b3
#define ICON_FA_FILE                    u8"\xef\x85\x9b"   // f15b
#define ICON_FA_FOLDER                  u8"\xef\x81\xbb"   // f07b
#define ICON_FA_FOLDER_OPEN             u8"\xef\x81\xbc"   // f07c
#define ICON_FA_LAYER_GROUP             u8"\xef\x97\xbd"   // f5fd
#define ICON_FA_OBJECT_GROUP            u8"\xef\x89\x87"   // f247
#define ICON_FA_OBJECT_UNGROUP          u8"\xef\x89\x88"   // f248
#define ICON_FA_LIST                    u8"\xef\x80\xba"   // f03a
#define ICON_FA_TAG                     u8"\xef\x80\xab"   // f02b
#define ICON_FA_TAGS                    u8"\xef\x80\xac"   // f02c
#define ICON_FA_COPY_SQUARE             ICON_FA_COPY

// ---- UI / Eye ----
#define ICON_FA_EYE                     u8"\xef\x81\xae"   // f06e
#define ICON_FA_EYE_SLASH               u8"\xef\x81\xb0"   // f070
#define ICON_FA_LOCK                    u8"\xef\x80\xa3"   // f023
#define ICON_FA_LOCK_OPEN               u8"\xef\x8f\x81"   // f3c1
#define ICON_FA_EXPAND_ALT              ICON_FA_EXPAND
#define ICON_FA_COMPRESS_ALT            ICON_FA_COMPRESS

// ---- Session / Network ----
#define ICON_FA_USER                    u8"\xef\x80\x87"   // f007
#define ICON_FA_USERS                   u8"\xef\x83\x80"   // f0c0
#define ICON_FA_GLOBE                   u8"\xef\x82\xac"   // f0ac
#define ICON_FA_SERVER                  u8"\xef\x88\xb3"   // f233
#define ICON_FA_NETWORK_WIRED           u8"\xef\x9b\xbf"   // f6ff
#define ICON_FA_MAP                     u8"\xef\x89\xb9"   // f279
#define ICON_FA_LOCATION_DOT            u8"\xef\x8f\x85"   // f3c5

// ---- Game / Tools ----
#define ICON_FA_GAMEPAD                 u8"\xef\x84\x9b"   // f11b
#define ICON_FA_CROSSHAIRS              u8"\xef\x81\x9b"   // f05b
#define ICON_FA_CAMERA                  u8"\xef\x80\xb0"   // f030
#define ICON_FA_GEAR                    u8"\xef\x80\x93"   // f013
#define ICON_FA_GEARS                   u8"\xef\x82\x85"   // f085
#define ICON_FA_WRENCH                  u8"\xef\x82\xad"   // f0ad
#define ICON_FA_HAMMER                  u8"\xef\x9b\xa3"   // f6e3
#define ICON_FA_PAINTBRUSH              u8"\xef\x87\xbc"   // f1fc
#define ICON_FA_CHART_BAR               u8"\xef\x82\x80"   // f080
#define ICON_FA_CLOCK                   u8"\xef\x80\x97"   // f017
#define ICON_FA_HOUSE                   u8"\xef\x80\x95"   // f015

// ---- Playback ----
#define ICON_FA_PLAY                    u8"\xef\x81\x8b"   // f04b
#define ICON_FA_PAUSE                   u8"\xef\x81\x8c"   // f04c
#define ICON_FA_STOP                    u8"\xef\x81\x8d"   // f04d
#define ICON_FA_BACKWARD                u8"\xef\x81\x8a"   // f04a
#define ICON_FA_FORWARD                 u8"\xef\x81\x8e"   // f04e
