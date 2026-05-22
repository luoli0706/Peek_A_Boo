cmake_minimum_required(VERSION 3.20)
project(Peek-A-Boo-Server C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Server executable (ENet built directly from source + protobuf messages)
find_package(Protobuf REQUIRED)

set(PROTO_FILE ${CMAKE_CURRENT_SOURCE_DIR}/proto/peekaboo.proto)
set(GEN_DIR ${CMAKE_CURRENT_BINARY_DIR}/generated)
set(GEN_SRC ${GEN_DIR}/peekaboo.pb.cc)
set(GEN_HDR ${GEN_DIR}/peekaboo.pb.h)

add_custom_command(
    OUTPUT ${GEN_SRC} ${GEN_HDR}
    COMMAND ${CMAKE_COMMAND} -E make_directory ${GEN_DIR}
    COMMAND ${Protobuf_PROTOC_EXECUTABLE}
            --cpp_out=${GEN_DIR}
            --proto_path=${CMAKE_CURRENT_SOURCE_DIR}/proto
            ${PROTO_FILE}
    DEPENDS ${PROTO_FILE}
    VERBATIM
)

add_executable(server
    src/main.cpp
    src/protocol.cpp
    ${GEN_SRC}
    third_party/enet/enet.c
)

target_include_directories(server PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
    ${GEN_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party/enet
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party/enet/include
    ${Protobuf_INCLUDE_DIRS}
)

target_link_libraries(server PRIVATE ${Protobuf_LIBRARIES})

if (WIN32)
    target_link_libraries(server PRIVATE ws2_32 winmm)
endif()
