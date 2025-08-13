-- create database if not exists data_checker;

create table if not exists rules (
    id serial primary key,
    sql text not null,
    fail_template text not null,
    created_at timestamp not null default now(),
    is_valid boolean,
    validation_result text,
    validation_rule_stack_trace text,
    validation_data_stack_trace text
);
comment on table rules is 'Правила проверки данных';
comment on column rules.sql is 'SQL запрос';
comment on column rules.fail_template is 'Шаблон сообщения об ошибке';
comment on column rules.created_at is 'Дата и время создания правила';
comment on column rules.is_valid is 'Флаг валидности правила';
comment on column rules.validation_result is 'Результат валидации правила';
comment on column rules.validation_rule_stack_trace is 'Стек вызовов правил';
comment on column rules.validation_data_stack_trace is 'Стек вызовов данных';


create table if not exists validation_results (
    id serial primary key,
    rule_id int not null,
    object_id int not null,
    is_fail boolean not null default false,
    description text,
    date timestamp not null default now()
);
comment on table validation_results is 'Результаты валидации';
comment on column validation_results.id is 'Уникальный идентификатор результата';
comment on column validation_results.rule_id is 'Идентификатор правила';
comment on column validation_results.object_id is 'Идентификатор проверяемого объекта';
comment on column validation_results.is_fail is 'Флаг неуспешной проверки';
comment on column validation_results.description is 'Описание результата проверки';
comment on column validation_results.date is 'Дата и время выполнения проверки';


create table if not exists test_data (
    id serial primary key,
    object_id int not null,
    value_1 int not null,
    value_2 int not null,
    value_3 text not null,
    value_4 timestamp not null,
    value_5 timestamp not null,
    value_6 boolean not null
);
