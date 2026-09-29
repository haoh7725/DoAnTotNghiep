"""Run only against an isolated database populated by --seed-demo.
Set TEST_API_URL and TEST_SEED_PASSWORD. The test changes demo data and resets one demo password.
"""
import os,json,urllib.request,urllib.error,uuid,http.cookiejar
base=os.environ['TEST_API_URL'].rstrip('/')
password=os.environ['TEST_SEED_PASSWORD']
checks=0

def call(path,method='GET',body=None,token=None,status=200,header=True,opener=None):
 global checks
 headers={'Content-Type':'application/json'}
 if header: headers['X-Requested-With']='ResearchHub'
 if token: headers['Authorization']='Bearer '+token
 req=urllib.request.Request(base+'/api'+path,data=None if body is None else json.dumps(body).encode(),headers=headers,method=method)
 try:
  response=(opener.open(req) if opener else urllib.request.urlopen(req))
 except urllib.error.HTTPError as e: response=e
 raw=response.read()
 assert response.status==status, f'{method} {path}: expected {status}, got {response.status}: {raw.decode()[:600]}'
 checks+=1
 return json.loads(raw) if raw else None

call('/auth/me',status=401)
call('/auth/login','POST',{'username':'demo.quan_tri','password':password},status=400,header=False)
call('/auth/login','POST',{'username':'demo.quan_tri','password':'wrong'},status=401)
roles=['quan_tri','giang_vien','truong_bo_mon','truong_khoa','phong_qlkh','ban_giam_hieu']
tokens={r:call('/auth/login','POST',{'username':'demo.'+r,'password':password})['accessToken'] for r in roles}
admin=tokens['quan_tri']
profiles={r:call('/auth/me',token=t) for r,t in tokens.items()}
for r,t in tokens.items():
 assert profiles[r]['permissions'][0]['role']==r.upper()
 call('/admin/accounts',token=t,status=200 if r=='quan_tri' else 403)
 call('/lookups',token=t)
call('/auth/me',token=admin[:-10]+'aaaaaaaaaa',status=401)
jar=http.cookiejar.CookieJar();opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
call('/auth/login','POST',{'username':'demo.quan_tri','password':password},opener=opener)
call('/auth/me',opener=opener)
call('/auth/logout','POST',opener=opener,status=204)
call('/auth/me',opener=opener,status=401)
call('/auth/login','POST',{'username':'demo.quan_tri','password':password},status=429)
tag=uuid.uuid4().hex[:8]
f=call('/admin/faculties','POST',{'code':tag,'name':'Test faculty'},admin,201)
call('/admin/faculties','POST',{'code':tag,'name':'Duplicate'},admin,409)
call('/admin/faculties','POST',{'code':'  ','name':'Bad'},admin,400)
call('/admin/faculties/'+str(f['id']),'PUT',{'code':tag,'name':'Updated'},admin)
d=call('/admin/departments','POST',{'facultyId':f['id'],'code':tag,'name':'Department'},admin,201)
call('/admin/faculties/'+str(f['id']),'DELETE',token=admin,status=409)
call('/admin/departments','POST',{'facultyId':999999,'code':tag+'x','name':'Bad'},admin,409)
call('/admin/academic-years','POST',{'code':tag,'startDate':'2027-01-01','endDate':'2026-01-01'},admin,400)
y=call('/admin/academic-years','POST',{'code':tag,'startDate':'2026-01-01','endDate':'2026-12-31'},admin,201)
call('/admin/academic-years/'+str(y['id']),'PUT',{'code':tag,'startDate':'2026-02-01','endDate':'2026-12-31'},admin)
u=call('/admin/accounts','POST',{'username':'test.'+tag,'fullName':'Test User','email':tag+'@example.test','password':password},admin,201)
assert 'passwordHash' not in u
uid=u['id']; path='/admin/accounts/'+str(uid)
call(path,'PUT',{'fullName':'Renamed','email':None,'status':'HOAT_DONG'},admin)
call(path+'/permissions','PUT',[{'role':'GIANG_VIEN','scope':'TOAN_TRUONG','facultyId':None,'departmentId':None}],admin,400)
call(path+'/permissions','PUT',[{'role':'TRUONG_BO_MON','scope':'BO_MON','facultyId':f['id']+999,'departmentId':d['id']}],admin,400)
perm={'role':'TRUONG_BO_MON','scope':'BO_MON','facultyId':f['id'],'departmentId':d['id']}
call(path+'/permissions','PUT',[perm],admin,204)
call(path+'/permissions','PUT',[perm,perm],admin,400)
assert len(call(path+'/permissions',token=admin))==1
call('/admin/accounts/'+str(profiles['quan_tri']['id'])+'/permissions','PUT',[],admin,400)
# Permissions and account state must be reloaded on an already-issued token.
gvid=profiles['giang_vien']['id']; gvpath='/admin/accounts/'+str(gvid)
call(gvpath+'/permissions','PUT',[perm],admin,204)
scoped=call('/lookups',token=tokens['giang_vien'])
assert d['id'] in [x['id'] for x in scoped['departments']]
call(gvpath+'/permissions','PUT',[{'role':'GIANG_VIEN','scope':'CA_NHAN','facultyId':None,'departmentId':None}],admin,204)
scoped=call('/lookups',token=tokens['giang_vien'])
assert d['id'] not in [x['id'] for x in scoped['departments']]
call(gvpath,'PUT',{'fullName':profiles['giang_vien']['fullName'],'email':None,'status':'KHOA'},admin)
call('/auth/me',token=tokens['giang_vien'],status=401)
call(gvpath,'PUT',{'fullName':profiles['giang_vien']['fullName'],'email':None,'status':'HOAT_DONG'},admin)
call('/auth/me',token=tokens['giang_vien'])
call(gvpath+'/password','PUT',{'password':password},admin,204)
call('/auth/me',token=tokens['giang_vien'],status=401)
call(path,'DELETE',token=admin,status=204)
call('/admin/departments/'+str(d['id']),'DELETE',token=admin,status=204)
call('/admin/faculties/'+str(f['id']),'DELETE',token=admin,status=204)
call('/admin/academic-years/'+str(y['id']),'DELETE',token=admin,status=204)
print(f'PASS: {checks} HTTP assertions plus role, scope, password exclusion and permission-refresh assertions')
