insert into rules (id, sql, fail_template)
values  (1, '
select  object_id
,       (count(*) filter (where value_1 > 10)) > 1 as is_fail
from test_data
group by object_id
', 'Пользователь не найден')
,       (2, '
select  object_id
,       max(value_4) - min(value_5) > interval ''30 days'' as is_fail
from test_data
group by object_id
', 'Объект имеет диапазон дат между value_4 и value_5 больше 30 дней')
,       (3, '
select  object_id
,       bool_or(value_6) as is_fail
from test_data
group by object_id
', 'Объект имеет значение value_6 равное true')
,       (4, '
select  object_id
,       (count(*) filter (where value_1 > 45)) > 1 as is_fail
from test_data
group by object_id
', 'Объект имеет более одного значения value_1 больше 45')
,       (5, '
with 
valid_res as (
    select  object_id
    ,       (count(*) filter (where value_1 > 45)) > 1 as is_fail
    from test_data
    group by object_id    
)
select  object_id
,       is_fail
from valid_res
', '(CTE) Объект имеет более одного значения value_1 больше 45')
     ,       (6, '
insert into test_data (object_id, value_1, value_2, value_3, value_4, value_5, value_6) values (100, 45, ''value_2'', ''value_3'', ''value_4'', ''value_5'', ''value_6'')
', '')
     ,       (7, '
with ins as (
  insert into validation_results(rule_id, object_id, is_fail)
  select 999, object_id, true from test_data
  returning object_id, true as is_fail
)
select object_id, true as is_fail from ins
', 'DML через CTE (ModifyTable)')
,       (8, '
select object_id, true as is_fail
from test_data
for update
', 'Блокировка строк FOR UPDATE')
,       (9, '
select object_id, true as is_fail
from test_data
for share
', 'Блокировка строк FOR SHARE')
,       (10, '
select gs as object_id, true as is_fail
from generate_series(1, 10) as gs
', 'SRF в FROM (FunctionScan)')
,       (11, '
select generate_series(1, 5) as object_id, true as is_fail
', 'SRF в SELECT (ProjectSet)')
,       (12, '
select object_id, true as is_fail, pg_sleep(0.1)
from test_data
', 'Запрещенная функция pg_sleep')
,       (13, '
select object_id, (pg_read_file(''/etc/hosts'') is not null) as is_fail
from test_data
', 'Запрещенная функция pg_read_file')
,       (14, '
select 1 as object_id, exists(select 1 from pg_ls_dir(''/'') ) as is_fail
', 'Запрещенная функция pg_ls_dir')
,       (15, '
select 1 as object_id, exists(
  select 1 from dblink(''dbname=postgres'', ''select 1'') as t(x int)
) as is_fail
', 'Запрещенная функция dblink')
,       (16, '
select 1 as object_id, (lo_export(0, ''/tmp/x'') is not null) as is_fail
', 'Запрещенная функция lo_export')
,       (17, '
select 1 as object_id, pg_terminate_backend(pg_backend_pid()) as is_fail
', 'Запрещенная функция pg_terminate_backend')
,       (18, '
select object_id, exists(select 1 from information_schema.tables) as is_fail
from test_data
', 'Доступ к системной схеме information_schema')
,       (19, '
select object_id, exists(select 1 from pg_catalog.pg_tables) as is_fail
from test_data
', 'Доступ к системной схеме pg_catalog')
,       (20, '
select 1 as object_id, true as is_fail; drop table test_data
', 'Попытка многооператорной инъекции')
 ,       (21, '
select  object_id
,       (count(*) filter (where value_1 > 10)) > 1 as is_fail
from test_data
group by object_id
', 'Контрольный безопасный: COUNT FILTER')
 ,       (22, '
select  object_id
,       bool_or(value_6) as is_fail
from test_data
group by object_id
', 'Контрольный безопасный: bool_or')
,       (23, '
select object_id, true as is_fail
from test_data
for no key update
', 'Блокировка строк FOR NO KEY UPDATE')
,       (24, '
select object_id, true as is_fail
from test_data
for key share
', 'Блокировка строк FOR KEY SHARE')
,       (25, '
select object_id, (pg_read_binary_file(''/etc/hosts'', 0, 100) is not null) as is_fail
from test_data
', 'Запрещенная функция pg_read_binary_file')
,       (26, '
select 1 as object_id, (pg_stat_file(''/etc/hosts'') is not null) as is_fail
', 'Запрещенная функция pg_stat_file')
,       (27, '
select 1 as object_id, exists(select * from pg_logdir_ls(''log'')) as is_fail
', 'Запрещенная функция pg_logdir_ls')
,       (28, '
select 1 as object_id, exists(select * from pg_file_settings()) as is_fail
', 'Запрещенная функция pg_file_settings')
,       (29, '
select 1 as object_id, pg_reload_conf() as is_fail
', 'Запрещенная функция pg_reload_conf')
,       (30, '
select 1 as object_id, pg_cancel_backend(pg_backend_pid()) as is_fail
', 'Запрещенная функция pg_cancel_backend')
,       (31, '
select 1 as object_id, (lo_import(''/etc/hosts'') is not null) as is_fail
', 'Запрещенная функция lo_import')
,       (32, '
select 1 as object_id, exists(
  select 1 from dblink_exec(''dbname=postgres'', ''select 1'')
) as is_fail
', 'Запрещенная функция dblink_exec')
,       (33, '
select unnest(array[1,2,3]) as object_id, true as is_fail
', 'SRF в FROM (FunctionScan через unnest)')
,       (34, '
select x.object_id, true as is_fail
from json_to_recordset(''[{"object_id":1},{"object_id":2}]'') as x(object_id int)
', 'SRF в FROM (FunctionScan через json_to_recordset)')
,       (35, '
select regexp_split_to_table(''1,2,3'', '','')::int as object_id, true as is_fail
', 'SRF в SELECT (ProjectSet через regexp_split_to_table)')
,       (36, '
with upd as (
  update test_data set value_1 = value_1 where false
  returning object_id
)
select object_id, true as is_fail
from upd
', 'DML через CTE (ModifyTable: UPDATE RETURNING)')
,       (37, '
select object_id, true as is_fail
from test_data
where pg_sleep(0.01) is null
', 'Запрещенная функция в условии Filter: pg_sleep')
,       (38, '
select t1.object_id, true as is_fail
from test_data t1
join test_data t2
  on t1.id = t2.id
 and pg_sleep(0.01) is null
', 'Запрещенная функция в Join Filter: pg_sleep')
,       (39, '
select object_id, true as is_fail, pg_read_file(''/etc/hosts'')
from test_data
', 'Запрещенная функция в Output: pg_read_file')
,       (40, '
select object_id, true as is_fail
from test_data
order by pg_sleep(0.01)
', 'Запрещенная функция в Sort Key: pg_sleep')
on conflict (id) do nothing ;


