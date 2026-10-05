"""Tạo dữ liệu mẫu giảng viên và lý lịch khoa học trên database PHÁT TRIỂN.

Chạy sau khi dữ liệu nền (khoa, bộ môn) đã có. Mỗi bộ môn (tối đa DEMO_DEPARTMENTS, mặc định 3) nhận
DEMO_PER_DEPARTMENT giảng viên (mặc định 2), mỗi người có tài khoản GIANG_VIEN/CA_NHAN riêng. Chạy lại
được: bản ghi đã tồn tại thì bỏ qua.

Biến môi trường: API_URL, ADMIN_USERNAME (mặc định admin), ADMIN_PASSWORD, DEMO_PASSWORD (từ 12 ký tự).
"""
import json
import os
import re
import urllib.error
import urllib.request

BASE = os.environ["API_URL"].rstrip("/")
DEMO_PASSWORD = os.environ["DEMO_PASSWORD"]
MAX_DEPARTMENTS = int(os.environ.get("DEMO_DEPARTMENTS", "3"))
PER_DEPARTMENT = int(os.environ.get("DEMO_PER_DEPARTMENT", "2"))


def call(path, method="GET", body=None, token=None, ok=(200, 201, 204)):
    headers = {"Content-Type": "application/json", "X-Requested-With": "ResearchHub"}
    if token:
        headers["Authorization"] = "Bearer " + token
    request = urllib.request.Request(
        BASE + "/api" + path,
        data=None if body is None else json.dumps(body).encode(),
        headers=headers,
        method=method,
    )
    try:
        response = urllib.request.urlopen(request)
    except urllib.error.HTTPError as error:
        response = error
    raw = response.read()
    if response.status not in ok:
        raise RuntimeError(f"{method} {path}: {response.status} {raw.decode(errors='replace')[:400]}")
    return response.status, (json.loads(raw) if raw else None)


def find_account(username, token):
    page = 1
    while True:
        _, data = call(f"/admin/accounts?page={page}&pageSize=100", token=token)
        for item in data["items"]:
            if item["username"] == username:
                return item
        if page * 100 >= data["total"]:
            return None
        page += 1


_, login = call("/auth/login", "POST", {
    "username": os.environ.get("ADMIN_USERNAME", "admin"), "password": os.environ["ADMIN_PASSWORD"]})
admin = login["accessToken"]
_, lookups = call("/lookups", token=admin)
created = skipped = 0

for department in lookups["departments"][:MAX_DEPARTMENTS]:
    slug = re.sub(r"[^a-z0-9]+", "-", department["code"].lower()).strip("-") or str(department["id"])
    for n in range(1, PER_DEPARTMENT + 1):
        username = f"gv.{slug}.{n}"[:100]
        status, account = call("/admin/accounts", "POST", {
            "username": username, "fullName": f"Giảng viên mẫu {slug} {n}",
            "email": None, "password": DEMO_PASSWORD}, admin, ok=(201, 409, 400, 422))
        if status != 201:
            account = find_account(username, admin)
            if account is None:
                raise RuntimeError(f"Không tạo được hoặc tìm thấy tài khoản {username}")
        call(f"/admin/accounts/{account['id']}/permissions", "PUT", [
            {"role": "GIANG_VIEN", "scope": "CA_NHAN", "facultyId": None, "departmentId": None}], admin)
        status, lecturer = call("/lecturers", "POST", {
            "accountId": account["id"], "departmentId": department["id"],
            "code": f"GV{department['id']:03d}{n:02d}", "fullName": f"Giảng viên mẫu {slug} {n}",
            "gender": "NAM" if n % 2 else "NU", "email": None,
            "academicRank": "Phó giáo sư" if n == 1 else None,
            "degree": "Tiến sĩ" if n == 1 else "Thạc sĩ", "position": "Giảng viên"}, admin, ok=(201, 409))
        if status == 409:
            skipped += 1
            continue
        call(f"/lecturers/{lecturer['id']}/scientific-profile", "PUT", {
            "expertise": "Chuyên môn mẫu", "researchFields": "Lĩnh vực mẫu",
            "researchDirections": "Hướng nghiên cứu mẫu", "activitySummary": "Tóm tắt hoạt động mẫu"}, admin)
        created += 1
        print(f"Đã tạo {username} (giảng viên {lecturer['code']})")

print(f"Xong: tạo {created}, bỏ qua {skipped} giảng viên đã có.")
