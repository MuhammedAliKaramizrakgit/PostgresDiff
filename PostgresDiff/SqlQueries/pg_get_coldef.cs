using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDiff
{

    public class pg_get_coldef : ISQLQuery
    {
        public string sqlquerytext { get { return sqlq; } }
        public static string sqlq = @"-- DROP FUNCTION public.pg_get_coldef(text, text, text, bool);

CREATE OR REPLACE FUNCTION public.pg_get_coldef(in_schema text, in_table text, in_column text, oldway boolean DEFAULT false)
 RETURNS text
 LANGUAGE plpgsql
AS $function$
DECLARE
v_coldef     text;
v_dt1        text;
v_dt2        text;
v_dt3        text;
v_nullable   boolean;
v_position   int; 
v_identity   text; 
v_hasdflt    boolean; 
v_dfltexpr   text;

BEGIN
  IF oldway THEN 
    SELECT pg_catalog.format_type(a.atttypid, a.atttypmod) INTO v_coldef FROM pg_namespace n, pg_class c, pg_attribute a, pg_type t 
    WHERE n.nspname = in_schema AND n.oid = c.relnamespace AND c.relname = in_table AND a.attname = in_column and a.attnum > 0 AND a.attrelid = c.oid AND a.atttypid = t.oid ORDER BY a.attnum;
    -- RAISE NOTICE 'DEBUG: oldway=%',v_coldef;
  ELSE
    -- a.attrelid::regclass::text, a.attname
    -- Issue#32: bypass the following query which converts to serial and bypasses explicit sequence defs
    -- SELECT CASE WHEN a.atttypid = ANY ('{int,int8,int2}'::regtype[]) AND EXISTS (SELECT FROM pg_attrdef ad WHERE ad.adrelid = a.attrelid AND ad.adnum   = a.attnum AND 
	  -- pg_get_expr(ad.adbin, ad.adrelid) = 'nextval(''' || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass || '''::regclass)') THEN CASE a.atttypid 
	  -- WHEN 'int'::regtype  THEN 'serial' WHEN 'int8'::regtype THEN 'bigserial' WHEN 'int2'::regtype THEN 'smallserial' END ELSE format_type(a.atttypid, a.atttypmod) END AS data_type  
	  -- INTO v_coldef FROM pg_namespace n, pg_class c, pg_attribute a, pg_type t 
	  -- WHERE n.nspname = in_schema AND n.oid = c.relnamespace AND c.relname = in_table AND a.attname = in_column and a.attnum > 0 AND a.attrelid = c.oid AND a.atttypid = t.oid ORDER BY a.attnum;
	  -- RAISE NOTICE 'DEBUG: newway=%',v_coldef;
	  
	  -- WHERE n.nspname = 'sequences' AND n.oid = c.relnamespace AND c.relname = 'atable' AND a.attname = 'key' and a.attnum > 0 AND a.attrelid = c.oid AND a.atttypid = t.oid ORDER BY a.attnum;	  	  
	  --  data_type
		-- -----------
		--  serial

	  -- WHERE n.nspname = 'sequences' AND n.oid = c.relnamespace AND c.relname = 'vectors3' AND a.attname = 'id' and a.attnum > 0 AND a.attrelid = c.oid AND a.atttypid = t.oid ORDER BY a.attnum;	  
    -- data_type
    -- -----------
    -- bigint

    -- Issue#32: show integer types, not serial types as output
    SELECT a.atttypid::regtype AS dt1, format_type(a.atttypid, a.atttypmod) as dt2, t.typname as dt3, CASE WHEN not(a.attnotnull) THEN True ELSE False END AS nullable, 
    -- Issue#36: removed column attgenerated since we do not use it anywhere and not in PGv10
    -- a.attnum, a.attidentity, a.attgenerated, a.atthasdef, pg_get_expr(ad.adbin, ad.adrelid) dfltexpr 
    -- INTO v_dt1, v_dt2, v_dt3, v_nullable, v_position, v_identity, v_generated, v_hasdflt, v_dfltexpr 
    a.attnum, a.attidentity, a.atthasdef, pg_get_expr(ad.adbin, ad.adrelid) dfltexpr 
    INTO v_dt1, v_dt2, v_dt3, v_nullable, v_position, v_identity, v_hasdflt, v_dfltexpr 
    FROM pg_attribute a JOIN pg_class c ON (a.attrelid = c.oid) JOIN pg_type t ON (a.atttypid = t.oid) LEFT JOIN pg_attrdef ad ON (a.attrelid = ad.adrelid AND a.attnum = ad.adnum) 
    -- WHERE c.relkind in ('r','p') AND a.attnum > 0 AND NOT a.attisdropped AND c.relnamespace::regnamespace::text = in_schema AND c.relname = in_table AND a.attname = in_column;
    WHERE c.relkind in ('r','p') AND a.attnum > 0 AND NOT a.attisdropped AND c.relnamespace::regnamespace::text = quote_ident(in_schema) AND c.relname = in_table AND a.attname = in_column;
	  -- RAISE NOTICE 'schema=%  table=%  column=%  dt1=%  dt2=%  dt3=%  nullable=%  pos=%  identity=%   HasDefault=%  DeftExpr=%', in_schema, in_table, in_column, v_dt1,v_dt2,v_dt3,v_nullable,v_position,v_identity,v_hasdflt,v_dfltexpr;

	  --   WHERE c.relkind in ('r','p') AND a.attnum > 0 AND NOT a.attisdropped AND c.relnamespace::regnamespace::text = 'sequences' AND c.relname = 'atable' AND a.attname = 'key';
		--    dt1   |   dt2   | dt3  | nullable | attnum | attidentity | attgenerated | atthasdef |                      dfltexpr
		-- ---------+---------+------+----------+--------+-------------+--------------+-----------+-----------------------------------------------------
		--  integer | integer | int4 | f        |      1 |             |              | t         | nextval('sequences.explicitsequence_key'::regclass)
		
		--     WHERE c.relkind in ('r','p') AND a.attnum > 0 AND NOT a.attisdropped AND c.relnamespace::regnamespace::text = 'sequences' AND c.relname = 'vectors3' AND a.attname = 'id';
		--   dt1   |  dt2   | dt3  | nullable | attnum | attidentity | attgenerated | atthasdef | dfltexpr
		-- --------+--------+------+----------+--------+-------------+--------------+-----------+----------
		--  bigint | bigint | int8 | f        |      1 | d           |              | f         |

    -- Issue#32 handled in calling routine, not here
 	  -- CREATE TABLE atable (key integer NOT NULL default nextval('explicitsequence_key'), avalue text);
	  -- IF v_dfltexpr IS NULL OR v_dfltexpr = '' THEN
    -- v_coldef = v_dt1;
    v_coldef = v_dt2;
	  
  END IF;
  RETURN v_coldef;
END;
$function$
;
";
    }
}
