"""Smoke test for an isolated database initialized from the project schema.

Set TEST_API_URL, TEST_ADMIN_USERNAME and TEST_ADMIN_PASSWORD. The API must be
running with the same seeded administrator credentials. Never run against
production because the script creates and deletes test records.
"""
import json
import os
import urllib.error
import urllib.request
import uuid

BASE = os.environ["TEST_API_URL"].rstrip("/")
ADMIN_USERNAME = os.environ.get("TEST_ADMIN_USERNAME", "admin")
ADMIN_PASSWORD = os.environ["TEST_ADMIN_PASSWORD"]
checks = 0


def call(path, method="GET", body=None, token=None, status=200, custom_header=True):
    global checks
    headers = {"Content-Type": "application/json"}
    if custom_header:
        headers["X-Requested-With"] = "ResearchHub"
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
        f"{method} {path}: expected {status}, got {response.status}: "
        f"{raw.decode(errors='replace')[:600]}"
    )
    checks += 1
    return json.loads(raw) if raw else None


call("/auth/me", status=401)
call("/auth/login", "POST", {"username": ADMIN_USERNAME, "password": ADMIN_PASSWORD}, status=400, custom_header=False)
call("/auth/login", "POST", {"username": ADMIN_USERNAME, "password": "wrong"}, status=401)
login = call("/auth/login", "POST", {"username": ADMIN_USERNAME, "password": ADMIN_PASSWORD})
admin = login["accessToken"]
profile = call("/auth/me", token=admin)
assert any(role["role"] == "QUAN_TRI" for role in profile["roles"])
call("/admin/accounts", token=admin)
call("/lookups", token=admin)
call("/auth/me", token=admin[:-10] + "aaaaaaaaaa", status=401)

tag = uuid.uuid4().hex[:8]
faculty = call("/admin/faculties", "POST", {"code": tag, "name": "Test faculty"}, admin, 201)
call("/admin/faculties", "POST", {"code": tag, "name": "Duplicate"}, admin, 409)
department = call(
    "/admin/departments",
    "POST",
    {"facultyId": faculty["id"], "code": tag, "name": "Test department"},
    admin,
    201,
)
year = call(
    "/admin/academic-years",
    "POST",
    {"code": tag, "startDate": "2026-01-01", "endDate": "2026-12-31"},
    admin,
    201,
)
call(
    "/admin/academic-years",
    "POST",
    {"code": tag + "x", "startDate": "2027-01-01", "endDate": "2026-01-01"},
    admin,
    400,
)

account = call(
    "/admin/accounts",
    "POST",
    {
        "username": "test." + tag,
        "fullName": "Test user",
        "email": tag + "@example.test",
        "password": "ResearchHub-Test-2026!",
    },
    admin,
    201,
)
assert "passwordHash" not in account
account_path = "/admin/accounts/" + str(account["id"])
permission = {
    "role": "TRUONG_BO_MON",
    "scope": "BO_MON",
    "facultyId": faculty["id"],
    "departmentId": department["id"],
}
call(account_path + "/permissions", "PUT", [permission], admin, 204)
assert len(call(account_path + "/permissions", token=admin)) == 1

user_login = call(
    "/auth/login",
    "POST",
    {"username": account["username"], "password": "ResearchHub-Test-2026!"},
)
user = user_login["accessToken"]
call("/admin/accounts", token=user, status=403)
scoped = call("/lookups", token=user)
assert department["id"] in [item["id"] for item in scoped["departments"]]

call(account_path, "PUT", {"fullName": "Test user", "email": None, "status": "KHOA"}, admin)
call("/auth/login", "POST", {"username": account["username"], "password": "ResearchHub-Test-2026!"}, status=403)
call(account_path, "PUT", {"fullName": "Test user", "email": None, "status": "HOAT_DONG"}, admin)
call(account_path + "/password", "PUT", {"password": "ResearchHub-Test-2026-Reset!"}, admin, 204)
call("/auth/login", "POST", {"username": account["username"], "password": "ResearchHub-Test-2026!"}, status=401)
call("/auth/login", "POST", {"username": account["username"], "password": "ResearchHub-Test-2026-Reset!"})

call(account_path, "DELETE", token=admin, status=204)
call("/admin/departments/" + str(department["id"]), "DELETE", token=admin, status=204)
call("/admin/faculties/" + str(faculty["id"]), "DELETE", token=admin, status=204)
call("/admin/academic-years/" + str(year["id"]), "DELETE", token=admin, status=204)

print(f"PASS: {checks} HTTP assertions and scope checks")
