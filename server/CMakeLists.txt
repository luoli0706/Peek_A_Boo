cmake_minimum_required(VERSION 3.20)
project(Peek-A-Boo-Server CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ENET library
set(BUILD_SHARED_LIBS OFF CACHE BOOL "")
add_subdirectory(deps/enet)

# Server executable
add_executable(server
    src/main.cpp
    src/protocol.cpp
)

target_include_directories(server PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/deps/enet/include
)

target_link_libraries(server PRIVATE enet ws2_32 winmm)
