﻿using System.Collections;
using antflowcore.constant.enus;
using antflowcore.exception;
using antflowcore.util;
using antflowcore.vo;
using AntFlowCore.Vo;
using Microsoft.Extensions.Logging;

namespace antflowcore.service.processor.filter;

public class ConditionService : IConditionService
{
    private readonly ILogger<ConditionService> _logger;

    public ConditionService(ILogger<ConditionService> logger)
    {
        _logger = logger;
    }
    public bool CheckMatchCondition(BpmnNodeVo bpmnNodeVo, BpmnNodeConditionsConfBaseVo conditionsConf,
        BpmnStartConditionsVo bpmnStartConditionsVo,bool isDynamicConditionGateway)
    {
        String nodeId=bpmnNodeVo.NodeId;
        List<int> conditionParamTypeList = conditionsConf.ConditionParamTypes;
        if (ObjectUtils.IsEmpty(conditionParamTypeList)) {
            return false;
        }

        int index = 0;
        bool result = true;
        foreach (int conditionParam in conditionParamTypeList)
        {
            ConditionTypeEnum? conditionTypeEnum = ConditionTypeEnumExtensions.GetEnumByCode(conditionParam);
            if (conditionTypeEnum == null) {
                _logger.LogInformation("condition type is null,type:{}", conditionParam);
                result = false;
                break;
            }

            ConditionTypeAttributes conditionTypeAttributes = conditionTypeEnum.Value.GetAttributes();
            Type conditionJudgeClassType = conditionTypeAttributes.ConditionJudgeClass;
            IEnumerable conditionJudgeServices = ServiceProviderUtils.GetServices(typeof(IConditionJudge));
            if (conditionJudgeServices == null)
            {
                throw new AFBizException($"未能根据服务类型:{conditionJudgeClassType}找到对应服务,请检查是否存在或者是否已经注入");
            }
           
            IConditionJudge conditionJudge = null;
            int count = 0;
            //in fact each time one can only get one
           foreach (object conditionJudgeService in conditionJudgeServices)
           {
               if (count > 1)
               {
                   throw new AFBizException("there should be only condition judge service!");
               }

               if (conditionJudgeService.GetType() == conditionJudgeClassType)
               {
                   conditionJudge= (IConditionJudge)conditionJudgeService;
                   count++;
               }
               
           }

           if (conditionJudge == null)
           {
               throw new AFBizException(
                   $"can not find a condition judge service by provided type:{conditionJudgeClassType}");
           }

           try
           {
               if (!conditionJudge.Judge(nodeId, conditionsConf, bpmnStartConditionsVo,index))
               {
                   //if any condition judge service judge failed,then the whole result is failed
                   result = false;
                   break;
               }
           }
           catch (AFBizException e)
           {
               _logger.LogInformation($"condiiton judge business exception:{ e.Message}" );
               throw;
           }
           catch (Exception e)
           {
              _logger.LogInformation("conditionjudge error:{}",e);
               throw;
           }
           index++;
        }

        return result;
    }
}