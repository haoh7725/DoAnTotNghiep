"""Smoke test cho /api/lecturers và /api/product-types trên database kiểm thử riêng.

Đặt TEST_API_URL, TEST_ADMIN_USERNAME, TEST_ADMIN_PASSWORD (giống smoke_api.py). Script tạo rồi
xóa khoa, bộ môn, tài khoản, giảng viên và loại sản phẩm thử, nên không chạy trên dữ liệu thật.
"""
import json
import os
import urllib.error
import urllib.request
import uuid

BASE = os.environ["TEST_API_URL"].rstrip("/")
ADMIN_USERNAME = os.environ.get("TEST_ADMIN_USERNAME", "admin")
ADMIN_PASSWORD = os.environ["TEST_ADMIN_PASSWORD"]
PASSWORD = "ResearchHub-Test-2026!"
checks = 0


def call(path, method="GET", body=None, token=None, status=200):
    global checks
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
    assert response.status == status, (
        f"{method} {path}: expected {status}, got {response.status}: {raw.decode(errors='replace')[:600]}"
    )
    checks += 1
    return json.loads(raw) if raw else None


def login(username):
    return call("/auth/login", "POST", {"username": username, "password": PASSWORD})["accessToken"]


def make_account(name, permissions):
    account = call("/admin/accounts", "POST",
                   {"username": name, "fullName": name, "email": None, "password": PASSWORD}, admin, 201)
    call(f"/admin/accounts/{account['id']}/permissions", "PUT", permissions, admin, 204)
    return account


admin = call("/auth/login", "POST", {"username": ADMIN_USERNAME, "password": ADMIN_PASSWORD})["accessToken"]
tag = uuid.uuid4().hex[:8]
created = {"accounts": [], "lecturers": []}

faculty = call("/admin/faculties", "POST", {"code": tag, "name": "Khoa thử"}, admin, 201)
dept_a = call("/admin/departments", "POST", {"facultyId": faculty["id"], "code": tag + "A", "name": "Bộ môn A"}, admin, 201)
dept_b = call("/admin/departments", "POST", {"facultyId": faculty["id"], "code": tag + "B", "name": "Bộ môn B"}, admin, 201)

personal = [{"role": "GIANG_VIEN", "scope": "CA_NHAN", "facultyId": None, "departmentId": None}]
acc_a = make_account("gva." + tag, personal)
acc_b = make_account("gvb." + tag, personal)
acc_c = make_account("gvc." + tag, personal)  # chưa liên kết giảng viên
acc_head = make_account("head." + tag, [{"role": "TRUONG_BO_MON", "scope": "BO_MON",
                                         "facultyId": faculty["id"], "departmentId": dept_a["id"]}])
created["accounts"] = [acc_a, acc_b, acc_c, acc_head]
tok_a, tok_b, tok_c, tok_head = (login(a["username"]) for a in created["accounts"])

# --- Loại sản phẩm ---
types = call("/product-types", token=tok_a)
assert {"BAI_BAO", "DE_TAI", "SACH", "CHUNG_NHAN"} <= {t["code"] for t in types}
code = "T" + tag.upper()
call("/product-types", "POST", {"code": code, "name": "Loại thử"}, tok_a, 403)
call("/product-types", "POST", {"code": "chu_thuong", "name": "Sai mã"}, admin, 400)
new_type = call("/product-types", "POST", {"code": code, "name": "Loại thử"}, admin, 201)
call("/product-types", "POST", {"code": code, "name": "Trùng"}, admin, 409)
renamed = call(f"/product-types/{new_type['id']}", "PUT", {"name": "Tên mới"}, admin)
assert renamed["code"] == code and renamed["name"] == "Tên mới"
call(f"/product-types/{new_type['id']}", "DELETE", token=tok_a, status=403)
call(f"/product-types/{new_type['id']}", "DELETE", token=admin, status=204)
call(f"/product-types/{new_type['id']}", token=admin, status=404)

# --- Giảng viên ---
call("/lecturers/me", token=tok_a, status=404)
body = {"accountId": acc_a["id"], "departmentId": dept_a["id"], "code": "A" + tag, "fullName": "  Giảng viên A ",
        "gender": "NAM", "email": None, "academicRank": "PGS", "degree": "Tiến sĩ"}
call("/lecturers", "POST", body, tok_a, 403)
lec_a = call("/lecturers", "POST", body, admin, 201)
assert lec_a["fullName"] == "Giảng viên A" and lec_a["facultyId"] == faculty["id"]
lec_b = call("/lecturers", "POST", {**body, "accountId": acc_b["id"], "departmentId": dept_b["id"],
                                    "code": "B" + tag, "fullName": "Giảng viên B"}, admin, 201)
created["lecturers"] = [lec_a, lec_b]
call("/lecturers", "POST", {**body, "accountId": acc_c["id"]}, admin, 409)          # trùng mã
call("/lecturers", "POST", {**body, "code": "C" + tag}, admin, 409)                 # tài khoản đã liên kết
call("/lecturers", "POST", {**body, "accountId": acc_c["id"], "code": "C" + tag, "departmentId": 99999999}, admin, 422)
call("/lecturers", "POST", {**body, "accountId": acc_c["id"], "code": "C" + tag, "gender": "X"}, admin, 400)

assert call("/lecturers/me", token=tok_a)["id"] == lec_a["id"]
call(f"/lecturers/{lec_a['id']}", token=tok_a)
call(f"/lecturers/{lec_b['id']}", token=tok_a, status=403)
call(f"/lecturers/{lec_a['id']}", token=tok_head)
call(f"/lecturers/{lec_b['id']}", token=tok_head, status=403)

assert call("/lecturers?keyword=" + tag, token=tok_a)["totalCount"] == 1
assert [i["id"] for i in call("/lecturers?keyword=" + tag, token=tok_head)["items"]] == [lec_a["id"]]
both = call(f"/lecturers?facultyId={faculty['id']}", token=admin)
assert both["totalCount"] == 2
call("/lecturers?pageSize=101", token=admin, status=400)

mine = call("/lecturers/me", "PUT", {"fullName": "Tên tự sửa", "gender": "NU"}, tok_a)
assert mine["fullName"] == "Tên tự sửa" and mine["code"] == "A" + tag and mine["academicRank"] == "PGS"
call(f"/lecturers/{lec_a['id']}", "PUT", {"departmentId": dept_a["id"], "code": "A" + tag, "fullName": "X"}, tok_a, 403)
moved = call(f"/lecturers/{lec_a['id']}", "PUT",
             {"departmentId": dept_b["id"], "code": "A" + tag, "fullName": "Giảng viên A"}, admin)
assert moved["departmentId"] == dept_b["id"]
call(f"/lecturers/{lec_a['id']}", "PUT",
     {"departmentId": dept_a["id"], "code": "A" + tag, "fullName": "Giảng viên A"}, admin)

# --- Lý lịch khoa học ---
empty = call("/lecturers/me/scientific-profile", token=tok_a)
assert empty["updatedAt"] is None and empty["expertise"] is None
saved = call("/lecturers/me/scientific-profile", "PUT",
             {"expertise": "Phần mềm", "researchFields": " AI ", "activitySummary": ""}, tok_a)
assert saved["researchFields"] == "AI" and saved["activitySummary"] is None and saved["updatedAt"]
call("/lecturers/me/scientific-profile", "PUT", {"expertise": "Phần mềm 2"}, tok_a)
assert call(f"/lecturers/{lec_a['id']}/scientific-profile", token=tok_head)["expertise"] == "Phần mềm 2"
call(f"/lecturers/{lec_a['id']}/scientific-profile", "PUT", {"expertise": "x"}, tok_head, 403)
call(f"/lecturers/{lec_a['id']}/scientific-profile", "PUT", {"expertise": "Do phòng QLKH"}, admin)
call(f"/lecturers/{lec_a['id']}/scientific-profile", token=tok_b, status=403)
call("/lecturers/me/scientific-profile", "PUT", {"expertise": "x" * 4001}, tok_a, 400)

# --- Xóa ---
call(f"/lecturers/{lec_a['id']}", "DELETE", token=tok_a, status=403)
for lecturer in created["lecturers"]:
    call(f"/lecturers/{lecturer['id']}", "DELETE", token=admin, status=204)
call(f"/lecturers/{lec_a['id']}", token=admin, status=404)
for account in created["accounts"]:
    call(f"/admin/accounts/{account['id']}", "DELETE", token=admin, status=204)
for department in (dept_a, dept_b):
    call(f"/admin/departments/{department['id']}", "DELETE", token=admin, status=204)
call(f"/admin/faculties/{faculty['id']}", "DELETE", token=admin, status=204)

print(f"PASS: {checks} HTTP assertions cho giảng viên, lý lịch khoa học và loại sản phẩm")
