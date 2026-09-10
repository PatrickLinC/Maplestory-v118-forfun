public class OpProbe {
  public static void main(String[] a) throws Exception {
    String[] names = {"CLOSE_RANGE_ATTACK","RANGED_ATTACK","MAGIC_ATTACK","SUMMON_ATTACK","SPECIAL_MOVE"};
    Class<?> c = Class.forName("handling.RecvPacketOpcode");
    // find a getValue-like method
    java.lang.reflect.Method gv=null;
    for (java.lang.reflect.Method m : c.getMethods()) {
      if (m.getParameterTypes().length==0 && (m.getReturnType()==int.class||m.getReturnType()==short.class)
          && (m.getName().toLowerCase().contains("value")||m.getName().equals("getValue"))) { gv=m; break; }
    }
    for (String n : names) {
      try {
        Object e = Enum.valueOf((Class<Enum>)c.asSubclass(Enum.class), n);
        int val = gv!=null ? ((Number)gv.invoke(e)).intValue() : -999;
        System.out.printf("%-20s = %d (0x%X)%n", n, val, val);
      } catch (Throwable t) { System.out.println(n+" -> "+t); }
    }
    if (gv!=null) System.out.println("(value method: "+gv.getName()+")");
  }
}
