using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDiff
{
    public   interface ISQLQuery
    {
        static string sqlquerytext { get; }
    }
    public class FuncViewTableDef : ISQLQuery
    {
        public string sqlquerytext { get { return sqlq; } }
        public static string sqlq = @"
--drop function if exists public.funcviewTableDef(she text,   gobject_type text  default null,  gobjid oid default null, includedtables bool default false )

CREATE OR REPLACE FUNCTION public.funcviewTableDef(she text,   gobject_type text  default null,  gobjid oid default null, includedtables bool default false )
RETURNS TABLE(alttip text, objectadi text, sqltext character varying, rsqltext character varying)
LANGUAGE plpgsql
AS $function$
DECLARE
fun_rec record;
fun_rec2 record;
  l_rec record;
  l_stmt text;
  donsql text[] ;
gelensql text[];
_sql text;
_connectionst2 text;
--elem int4 = 0;
donussql text;
tektext text;
a text;
arraysqlg jsonb;
arraysql text[];
begin
if gobject_type is  null or gobject_type = LOWER('FUNCTION') 
     or gobject_type <> LOWER('AGGREGATE') or gobject_type = LOWER('PROCEDURE') then 
for fun_rec in
(
SELECT
CASE prokind
                              WHEN 'f' THEN 'FUNCTION'
                              WHEN 'a' THEN 'AGGREGATE'
                              WHEN 'p' THEN 'PROCEDURE'
                              WHEN 'w' THEN 'FUNCTION'  -- window function (rarely applicable)
                               ELSE NULL      -- not possible in pg 11
                            end as alttip,
          (format('DROP %s %s %s;'
                          , CASE prokind
                              WHEN 'f' THEN 'FUNCTION if exists'
                              WHEN 'a' THEN 'AGGREGATE'
                              WHEN 'p' THEN 'PROCEDURE'
                              WHEN 'w' THEN 'FUNCTION'  -- window function (rarely applicable)
                               ELSE NULL      -- not possible in pg 11
                            END
                          , p.oid::regprocedure, '  ')
                   || E'\n') rsqltext
                   , (SELECT pg_get_functiondef((p.oid::regprocedure)::regproc)) as sqltext,
                 (p.oid::regprocedure)::text as objectname
              --
--select *
  FROM   pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
JOIN pg_language l ON l.oid = p.prolang
LEFT JOIN pg_extension e ON n.nspname = e.extname -- Extension fonksiyonlarını hariç tut
WHERE n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')  -- Sistem şemalarını hariç tut
  AND e.extname IS null  and  pronamespace = she::regnamespace  
and (gobject_type is  null or (gobject_type = LOWER(CASE prokind
                              WHEN 'f' THEN 'FUNCTION'
                              WHEN 'a' THEN 'AGGREGATE'
                              WHEN 'p' THEN 'PROCEDURE'
                              WHEN 'w' THEN 'FUNCTION'  -- window function (rarely applicable)
                               ELSE NULL      -- not possible in pg 11
                            END ))
     and gobjid = p.oid
     )
    AND    prokind = ANY ('{f,a,p,w}')   and probin is null
   and    proname <> 'funcviewal' and l.lanname = 'plpgsql'
and (not proname = any (array ['_insert2table',
'_i2utable',
'listtables',
'random_between',
'owneral',
'sorttablesbydependency',
'_update2table',
'view2func','rez_asyavalidate',
'drop_all_user_objects','beforefunc','fatura_asyavalidate', 'getmaliyethesap' ,'musteri_asyavalidate',
'ef2tablo']))
)
   
  loop
    
   IF fun_rec.sqltext IS NOT NULL THEN
     --   elem = elem + 1;
        alttip = fun_rec.alttip;
      sqltext =  fun_rec.sqltext ||';' || E'\n' ;
       rsqltext =   fun_rec.rsqltext;
       objectadi = she ||'.'||fun_rec.objectname ;
   return next ;
            
   ELSE
      RAISE NOTICE 'No fuctions found in schema %', quote_ident(fun_rec.objectname);
   END IF;
    end loop;
end if;



if gobject_type is  null or gobject_type = LOWER('VIEW') 
     then 
  
  for l_rec in (select schemaname, viewname
                from pg_views left join pg_class pc on pc.relname = viewname AND pc.relkind = 'v'
               where schemaname = she and viewname not like 'pg_st%' 
and  (gobject_type is  null or (gobject_type = LOWER('VIEW') 
     and gobjid = pc.oid )
     )
   )
  loop
     
   
     l_stmt :=  'CREATE OR REPLACE VIEW ' || l_rec.schemaname||'.'||l_rec.viewname || ' AS ' ||
  (select pg_get_viewdef(l_rec.schemaname||'.'||l_rec.viewname));
  -- elem = elem + 1;
   
   --donsql[elem] =   l_stmt   || E'\n' ;
   sqltext =  l_stmt   ;
   alttip = 'VIEW';
   objectadi = l_rec.schemaname||'.'||l_rec.viewname ;
    l_stmt := format('drop view if exists %I.%I ', l_rec.schemaname, l_rec.viewname);

   rsqltext =  l_stmt  ||';' || E'\n' ;

    --raise notice 'sqltext-- %' , sqltext;
 
   return next ;
  
 
   
  end loop;
end if ;

if gobject_type is  null or gobject_type = LOWER('TRIGGER') 
     then 
   raise notice 'TRRRRRR';
  for l_rec in 
  (SELECT 
    tg.tgname AS trigger_name, 
    n.nspname AS schema_name, 
    c.relname AS table_name,
    pg_get_triggerdef(tg.oid) AS create_trigger_query, 
    'DROP TRIGGER IF EXISTS ' || quote_ident(tg.tgname) || 
    ' ON ' || quote_ident(n.nspname) || '.' || quote_ident(c.relname) || ';' AS drop_trigger_query
FROM pg_trigger tg
JOIN pg_class c ON tg.tgrelid = c.oid
JOIN pg_namespace n ON c.relnamespace = n.oid
WHERE NOT tg.tgisinternal -- Sistem triggerlarını filtrele
and (gobject_type is  null or (gobject_type = LOWER('TRIGGER') 
     and gobjid = tg.oid )
     )
ORDER BY schema_name, table_name, trigger_name
               )
  loop
     
   
  
   sqltext =  l_rec.create_trigger_query   || ';' ||  E'\n' ;
   alttip = 'TRIGGER';
   objectadi = l_rec.schema_name||'.'||l_rec.trigger_name ;
   
 
   rsqltext =  l_rec.drop_trigger_query ||';' || E'\n' ;

 
 
   return next ;
  
 
   
  end loop;
end if;
-- raise notice '%' ,donsql ;
--- sadece publicleri alıyor parametreden al
if includedtables then  
-- raise notice 'TABLE DEFINATIONS';
for l_rec in 
     (select def tdef, in_table tabadi from pg_get_tabledeftum('public'::varchar , false)  
      )
  loop
     
   
  
   sqltext =  l_rec.tdef;
   alttip = 'TABLE';
   objectadi = l_rec.tabadi ;
   
 
   rsqltext = 'DROP TABLE if exists  ' ||  l_rec.tabadi  ;

 
 
   return next ;
  
 
   
  end loop;
end if;
end;
$function$
;
";
    }

}
