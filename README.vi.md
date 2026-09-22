# TeachLens 1.0 “Acorn”

**Biến từng điểm ảnh thành bài giảng.**

[English](README.md) · [Dự án trên GitHub](https://github.com/Thanhvh/TeachLens)

TeachLens là tiện ích Windows x64 nhỏ gọn dành cho dạy học và trình diễn phần mềm. Ứng dụng giúp người xem tập trung đúng chỗ mà không chiếm toàn bộ màn hình.

## Chức năng

- Làm nổi bật con trỏ bằng vòng xanh sạch, kèm hiệu ứng nhẹ khi bấm chuột.
- Làm tối đúng màn hình chứa con trỏ và chừa vùng spotlight hình tròn.
- Phóng đại một vùng nhỏ với mức 1.5×, 2.0×, 2.5× hoặc 3.0× bằng Windows Magnification API.
- Dùng kính tròn làm mặc định; vẫn có tùy chọn chữ nhật bo góc.
- Giữ kính và vùng nguồn trong màn hình hiện tại, kể cả bố cục nhiều màn hình có tọa độ âm.
- Nhận biết DPI theo từng màn hình và không chặn thao tác chuột.
- Dùng `Esc` để tắt chức năng TeachLens được bật gần nhất.
- Chạy ở khay hệ thống và lưu thiết lập tại `%LOCALAPPDATA%\TeachLens\settings.ini`.
- Chuyển đổi English/Tiếng Việt ngay trên cửa sổ chính. Ngôn ngữ mặc định là English.

## Cách chạy

1. Mở `TeachLens.exe`.
2. Chọn hình dạng kính, mức phóng và kích thước.
3. Có thể bấm **Ẩn xuống khay**; phím tắt vẫn hoạt động trong các phần mềm khác.

Không cần cài đặt và không cần quyền quản trị viên.

## Phím tắt

| Phím | Chức năng |
|---|---|
| `Ctrl + Alt + 1` | Bật/tắt highlight con trỏ |
| `Ctrl + Alt + 2` | Bật/tắt spotlight |
| `Ctrl + Alt + M` | Bật/tắt kính lúp |
| `Ctrl + Alt + 0` | Tắt mọi lớp phủ |
| `Ctrl + Alt + ↑` | Tăng mức phóng |
| `Ctrl + Alt + ↓` | Giảm mức phóng |
| `Esc` | Tắt chức năng TeachLens vừa bật |

`Esc` chỉ được đăng ký khi có ít nhất một lớp phủ đang bật. Nếu `Ctrl + Alt + M` bị ứng dụng khác chiếm, TeachLens tự chuyển sang `Ctrl + Alt + L` và hiển thị ghi chú.

## Quyền riêng tư và an toàn

- Không thu thập dữ liệu hoặc telemetry.
- Không truy cập mạng.
- Không quảng cáo hoặc phần mềm đi kèm.
- Không yêu cầu quyền quản trị viên.
- Thiết lập chỉ lưu trên máy của bạn.

TeachLens hiện chưa ký số nên Windows SmartScreen có thể cảnh báo ở lần chạy đầu. Chỉ tải bản phát hành từ trang GitHub của dự án và đối chiếu mã SHA-256 khi có.

## Yêu cầu

- Windows 10 hoặc Windows 11, 64-bit.
- .NET Framework 4.8 trở lên.

Nội dung có DRM, màn hình UAC bảo mật hoặc một số lớp hiển thị đặc biệt có thể không xuất hiện trong kính lúp.

## Ghi công

- Tác giả: **Vũ Hữu Thành**
- Email: **thanh.vuh@gmail.com**
- GitHub: **https://github.com/Thanhvh**
- Hỗ trợ kỹ thuật: **OpenAI Codex**

Codex được ghi nhận ở vai trò hỗ trợ kỹ thuật, không phải đồng tác giả là con người hoặc chủ thể pháp lý. Bản quyền thuộc Vũ Hữu Thành.

## Phiên bản

Phiên bản công khai: **1.0**  
Mật danh: **Acorn** — một khởi đầu nhỏ để lớn lên.

## Giấy phép

TeachLens được phát hành theo giấy phép MIT.
